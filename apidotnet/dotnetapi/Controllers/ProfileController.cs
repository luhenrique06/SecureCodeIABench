﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using dotnetapi.Core.DTO.Others.Response.Failed;
using dotnetapi.Core.DTO.Others.Response.Success;
using dotnetapi.Core.DTO.Others.Response.UserProfile;
using dotnetapi.Core.DTO.Profile;
using dotnetapi.Core.Entities.Profile;
using dotnetapi.Infrastructure.Repositories.Account;
using dotnetapi.Infrastructure.Repositories.Profile;
using dotnetapi.Infrastructure.Utility.JWT;

namespace dotnetapi.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProfileController : Controller
{
    private readonly AccountRepository _accountRepository;
    private readonly JWTAuthManager _jwtAuthManager;
    private readonly IMapper _mapper;
    private readonly ProfileRepository _profileRepository;

    public ProfileController(AccountRepository accountRepository, ProfileRepository profileRepository,
        JWTAuthManager jwtAuthManager, IMapper mapper)
    {
        _accountRepository = accountRepository;
        _profileRepository = profileRepository;
        _jwtAuthManager = jwtAuthManager;
        _mapper = mapper;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> GetProfile([FromQuery] ProfileSearchRequest request)
    {
        if (request.Email is null)
            return BadRequest(new FailedResponse { Message = "Please enter Email and try again." });
        
        var profile = await _profileRepository.GetProfileByEmail(request.Email);
        return profile is null
            ? BadRequest(new FailedResponse { Message = "There is no Profile." })
            : Ok(_mapper.Map<ProfileResponse>(profile));
    }
    

    [Authorize(Roles = "Admin,User")]
    [HttpGet("ShowUserProfile")]
    public async Task<ActionResult> ShowUserProfile()
    {
        var userId = _jwtAuthManager.TakeUserIdFromJWT(Request.Headers["Authorization"].ToString().Split(" ")[1]);
        var user = await _accountRepository.GetByIdAsync(userId);

        if (user.Email is null)
            return BadRequest(new FailedResponse
                { Message = "There is an while getting user data. Please try later." });
        
        var profile = await _profileRepository.GetProfileByEmail(user.Email);
        if (profile is null)
            return BadRequest(new FailedResponse
                { Message = "User's profile fields are empty. Please first create it." });
        
        var response = new UserProfileResponse
        {
            Name = user.Name,
            Surname = user.Surname,
            Email = user.Email,
            Password = user.Password,
            Address = profile.Address,
            Birhtday = profile.Birthday.ToString(),
            Hobby = profile.Hobby
        };
        return Json(response);
    }
    

    [Authorize(Roles = "Admin,User")]
    [HttpPost]
    public async Task<ActionResult> CreateProfile([FromBody] ProfileRequest request)
    {
        var email = _jwtAuthManager.TakeEmailFromJWT(Request.Headers["Authorization"].ToString().Split(" ")[1]);
        var profile = await _profileRepository.GetProfileByEmail(email);

        if (profile is not null) return BadRequest(new FailedResponse { Message = "Profile is already created." });
        
        var createProfile = _mapper.Map<ProfileEntity>(request);
        createProfile.Email = email;
        var prof = await _profileRepository.CreateAsync(createProfile);
        return Ok(_mapper.Map<ProfileResponse>(prof));

    }


    [HttpPut]
    [Authorize(Roles = "Admin,User")]
    public async Task<ActionResult> UpdateProfile([FromBody] ProfileRequest request)
    {
        var email = _jwtAuthManager.TakeEmailFromJWT(Request.Headers["Authorization"].ToString().Split(" ")[1]);
        var profile = await _profileRepository.GetProfileByEmail(email);

        if (profile.Id is null)
            return BadRequest(new FailedResponse
                { Message = "Profile is not created yet. This process can be done after profile is created." });
        
        var updateProile = _mapper.Map<ProfileEntity>(request);
        updateProile.Email = email;
        updateProile.Id = profile.Id;
        await _profileRepository.UpdateAsync(profile.Id, updateProile);
        return Ok(_mapper.Map<ProfileResponse>(request));

    }

    [HttpDelete]
    [AllowAnonymous]
    public async Task<ActionResult> DeleteProfile([FromQuery] ProfileSearchRequest request)
    {
        if (request.Email is null)
            return BadRequest(new FailedResponse
                { Message = "Please enter Email information and try again your request." });
        
        var profile = await _profileRepository.GetProfileByEmail(request.Email);
        if (profile is null)
            return BadRequest(new FailedResponse
                { Message = "Profile is not created yet. This process can be done after profile is created." });
            
        await _profileRepository.DeleteAsync(profile.Id);
        return Ok(new SuccessResponse { Message = "Profile is deleted" });

    }
}