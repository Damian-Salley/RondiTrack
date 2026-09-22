using RondiTrack.DTOs.Users;
using RondiTrack.Models;
using RondiTrack.DTOs.Stokvels;

namespace RondiTrack.Mappers;

public static class StokvelMapper
{
    // Maps a Stokvel model to a StokvelResponse DTO.
    public static StokvelResponse ToResponse(Stokvel stokvel)
    {
        return new StokvelResponse
        {
            Id = stokvel.Id,
            Name = stokvel.Name,
            ContributionAmount = stokvel.ContributionAmount,
            MemberCount = stokvel.Members.Count
        };
    }

    // Maps a CreateStokvelRequest DTO to a Stokvel model.
    public static Stokvel ToDomain(CreateStokvelRequest request)
    {
        return new Stokvel(
            request.Id,
            request.Name,
            request.ContributionAmount
        );
    }

    // Updates a Stokvel model with data from an UpdateStokvelRequest DTO.
    public static void Update(Stokvel stokvel, UpdateStokvelRequest request)
    {
        stokvel.UpdateDetails(
            request.Name,
            request.ContributionAmount
        );
    }
}