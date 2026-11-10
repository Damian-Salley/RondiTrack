using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using RondiTrack.Services;

namespace RondiTrack.Controllers;

[ApiController, Authorize, Route("api/v1")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class JoinPaymentClosingController(JoinPaymentClosingService service) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);
    public record InviteRequest(string Rules, string Schedule);
    public record CloseRequest(string Version);

    [HttpPost("stokvels/{stokvelId:int}/cycles/{cycleId:int}/contributions/{memberId:int}/confirm")]
    [EnableRateLimiting("Financial")]
    public async Task<ActionResult<RondiTrack.DTOs.Contributions.ContributionResponse>> Confirm(
        int stokvelId, int cycleId, int memberId)
        => Ok(RondiTrack.Mappers.ContributionMapper.ToResponse(
            await service.Confirm(stokvelId, cycleId, memberId, UserId)));

    [HttpPost("stokvels/{stokvelId:int}/invites")]
    public async Task<IActionResult> CreateInvite(int stokvelId, InviteRequest request)
        => Ok(await service.CreateInvite(stokvelId, UserId, request.Rules, request.Schedule));

    [HttpGet("invites/{code}"), EnableRateLimiting("Financial")]
    public async Task<IActionResult> PreviewInvite(string code)
        => Ok(await service.PreviewInvite(code, UserId));

    [HttpPost("invites/{code}/join"), EnableRateLimiting("Financial")]
    public async Task<IActionResult> Join(string code)
        => Ok(new { stokvelId = await service.Join(code, UserId) });

    [HttpGet("stokvels/{stokvelId:int}/cycles/{cycleId:int}/contributions/{memberId:int}/proof/details")]
    public async Task<IActionResult> ProofDetails(int stokvelId, int cycleId, int memberId)
        => Ok(await service.ProofDetails(stokvelId, cycleId, memberId, UserId));

    [HttpGet("stokvels/{stokvelId:int}/cycles/{cycleId:int}/contributions/{memberId:int}/proof")]
    public async Task<IActionResult> Download(int stokvelId, int cycleId, int memberId)
    {
        var proof = (await service.Download(stokvelId, cycleId, memberId, UserId))!;
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        var extension = proof.ContentType switch { "application/pdf" => "pdf", "image/png" => "png", _ => "jpg" };
        return File(proof.Bytes, proof.ContentType, $"proof.{extension}");
    }

    [HttpPost("stokvels/{stokvelId:int}/cycles/{cycleId:int}/contributions/{memberId:int}/proof")]
    [RequestSizeLimit(6 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(int stokvelId, int cycleId, int memberId, IFormFile file)
    {
        if (file.Length == 0 || file.Length > JoinPaymentClosingService.ProofLimit)
            return Problem(statusCode: 400, detail: "Choose a file up to 5 MiB.");
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        await service.SaveProof(stokvelId, cycleId, memberId, UserId, stream.ToArray());
        return Ok(new { message = "Proof uploaded." });
    }

    [HttpDelete("stokvels/{stokvelId:int}/cycles/{cycleId:int}/contributions/{memberId:int}/proof")]
    public async Task<IActionResult> Remove(int stokvelId, int cycleId, int memberId)
    {
        await service.SaveProof(stokvelId, cycleId, memberId, UserId, null);
        return NoContent();
    }

    [HttpGet("contributioncycles/{cycleId:int}/close-preview")]
    public async Task<IActionResult> PreviewClose(int cycleId)
        => Ok(await service.PreviewClose(cycleId, UserId));

    [HttpPost("contributioncycles/{cycleId:int}/close"), EnableRateLimiting("Financial")]
    public async Task<IActionResult> Close(int cycleId, CloseRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key)
        => Ok(await service.Close(cycleId, UserId, request.Version, key ?? ""));
}
