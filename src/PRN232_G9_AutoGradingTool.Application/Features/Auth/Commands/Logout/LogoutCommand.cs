using MediatR;
using PRN232_G9_AutoGradingTool.Application.Common.Models;

namespace PRN232_G9_AutoGradingTool.Application.Features.Auth.Commands.Logout;

public record LogoutCommand : IRequest<Result>;