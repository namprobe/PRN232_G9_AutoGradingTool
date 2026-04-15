using MediatR;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.Auth;
using PRN232_G9_AutoGradingTool.Application.Common.Models;

namespace PRN232_G9_AutoGradingTool.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand : IRequest<Result<AuthResponse>>;