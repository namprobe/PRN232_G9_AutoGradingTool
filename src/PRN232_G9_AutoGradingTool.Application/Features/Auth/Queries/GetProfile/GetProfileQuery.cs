using MediatR;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.Auth;

namespace PRN232_G9_AutoGradingTool.Application.Features.Auth.Queries.GetProfile;

public record GetProfileQuery : IRequest<Result<ProfileResponse>>;