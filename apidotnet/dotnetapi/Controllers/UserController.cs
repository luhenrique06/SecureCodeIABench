using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using dotnetapi.Core.DTO.Account;
using dotnetapi.Core.DTO.Others.Response.Failed;
using dotnetapi.Core.DTO.Others.Response.Success;
using dotnetapi.Core.DTO.User;
using dotnetapi.Core.Entities.Account;
using dotnetapi.Infrastructure.Repositories.Account;
using dotnetapi.Infrastructure.Utility.JWT;

namespace dotnetapi.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : Controller
{
    private readonly AccountRepository _accountRepository;
    private readonly JWTAuthManager _jwtAuthManager;
    private readonly IMapper _mapper;


    public UserController(AccountRepository accountRepository, JWTAuthManager jwtAuthManager, IMapper mapper)
    {
        _accountRepository = accountRepository;
        _jwtAuthManager = jwtAuthManager;
        _mapper = mapper;
    }

    [Authorize(Roles = "Admin,User")]
    [HttpGet]
    public async Task<ActionResult> GetAccount([FromQuery] string Email)
    {
        var account = await _accountRepository.GetAccountByEmail(Email);
        if (account is null)
            return BadRequest(
                new FailedResponse { Message = "There is no account with that email address." });

        var result = _mapper.Map<AccountResponse>(account);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,User")]
    [HttpPut]
    public async Task<ActionResult> Update([FromQuery] string Email, [FromBody] UpdateUserRequest request)
    {
        if (Email == "")
            return BadRequest(new FailedResponse { Message = "Email is empty, please fill it and try again." });

        var account = await _accountRepository.GetAccountByEmail(Email);
        if (account is null)
            return BadRequest(new FailedResponse { Message = "There is an error while getting Account information" });

        if (request.Name == "" || request.Password == "" || request.Surname == "")
            return BadRequest(new FailedResponse { Message = "All fields must bu filled" });
        
        await _accountRepository.UpdateAsync(account.Id, new AccountEntity
        {
            Id = account.Id,
            Name = request.Name,
            Surname = request.Surname,
            Email = account.Email,
            Password = request.Password,
            CreatedAt = account.CreatedAt,
            Role = account.Role
        });
        return Ok(new SuccessResponse { Message = "Account's updated" });
    }

    [Authorize(Roles = "Admin,User")]
    [HttpGet("Users")]
    public async Task<ActionResult> Users()
    {
        var accounts = await _accountRepository.GetAllAsync();
        if (accounts.Count == 0) return BadRequest(new FailedResponse { Message = "There is no profile" });

        return Ok(_mapper.Map<List<AccountResponse>>(accounts));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete]
    public async Task<ActionResult> DeleteAccount([FromQuery] string Email)
    {
        var account = await _accountRepository.GetAccountByEmail(Email);
        if (account.Id is null)
            return BadRequest(new FailedResponse
                { Message = "There is no account with start email address which you entered" });
        var result = _accountRepository.DeleteAccountByEmail(Email);
        return Ok(new SuccessResponse { Message = "User which you provided is deleted" });
    }
}