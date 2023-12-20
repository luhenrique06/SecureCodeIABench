using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using dotnetapi.Core.DTO.Card;
using dotnetapi.Core.DTO.Others.Response.Failed;
using dotnetapi.Core.DTO.Others.Response.Success;
using dotnetapi.Core.Entities.Card;
using dotnetapi.Infrastructure.Repositories.Card;
using dotnetapi.Infrastructure.Utility.JWT;

namespace dotnetapi.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CardController : Controller
{
    private readonly CardRepository _cardRepository;
    private readonly JWTAuthManager _jwtAuthManager;
    private readonly IMapper _mapper;
    
    public CardController(CardRepository cardRepository, JWTAuthManager jWtAuthManager, IMapper mapper)
    {
        _cardRepository = cardRepository;
        _jwtAuthManager = jWtAuthManager;
        _mapper = mapper;
    }
    [AllowAnonymous]
    [HttpGet]
    public ActionResult<string> GetCard()
    {
        var random = new Random();
        var randomNumber = random.Next(1, 10);

        if (randomNumber % 2 == 0)
            return Ok(new SuccessResponse
                { Message = "This endpoint is not working now. You should send request GetCardV2" });
        try
        {
            string? msg = null;
            msg.ToCharArray();
        }
        catch (Exception exception)
        {
            return BadRequest(new FailedResponse
                { Message = exception.Message + exception.StackTrace + exception.InnerException + exception.HelpLink });
        }

        return Ok("");
    }


    [Authorize(Roles = "Admin,User")]
    [HttpGet("GetCardV2")]
    public async Task<ActionResult> GetCardV2()
    {
        var userId = _jwtAuthManager.TakeUserIdFromJWT(Request.Headers["Authorization"].ToString().Split(" ")[1]);
        var card = await _cardRepository.GetCard(userId);
        return card is null ? Ok(new SuccessResponse { Message = "There is no card" }) : Ok(_mapper.Map<List<GetCardResponse>>(card));
    }

    [Authorize(Roles = "Admin,User")]
    [HttpPost]
    public async Task<ActionResult> CreateCard(CardCreateRequest request)
    {
        var userId = _jwtAuthManager.TakeUserIdFromJWT(Request.Headers["Authorization"].ToString().Split(" ")[1]);
        var card = _mapper.Map<CardEntity>(request);
        card.UserId = userId;
        await _cardRepository.CreateAsync(card);
        return Ok(new SuccessResponse { Message = "Card is created" });
    }
    

    [HttpDelete]
    [Authorize(Roles = "Admin,User")]
    public async Task<ActionResult> DeleteCard(DeleteCardRequest request)
    {
        if (request.CardId == null)
            return BadRequest(new FailedResponse
                { Message = "CardId is empty. Please enter CardId and try again your request." });
        
        var result = await _cardRepository.DeleteCard(request.CardId);
        return result
            ? Ok(new SuccessResponse { Message = "Card is deleted" })
            : BadRequest(new FailedResponse { Message = "There is a problem while card delete" });
    }
}