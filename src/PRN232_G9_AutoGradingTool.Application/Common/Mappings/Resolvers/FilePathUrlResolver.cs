using AutoMapper;
using Microsoft.Extensions.Configuration;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

namespace PRN232_G9_AutoGradingTool.Application.Common.Mappings.Resolvers;

/// <summary>
/// Universal AutoMapper value converter to convert relative file path to full URL
/// Can be used for any entity and any property that contains a file path
/// 
/// Usage with direct path property:
/// .ForMember(dest => dest.AvatarPath, opt => opt.ConvertUsing<FilePathUrlConverter, string?>(src => src.AvatarPath))
/// 
/// Usage with extracted path:
/// .ForMember(dest => dest.ProductImage, opt => opt.ConvertUsing<FilePathUrlConverter, string?>(src => ExtractPath(src)))
/// </summary>
public class FilePathUrlConverter : IValueConverter<string?, string?>
{
    private readonly IFileServiceFactory _fileServiceFactory;

    public FilePathUrlConverter(IFileServiceFactory fileServiceFactory)
    {
        _fileServiceFactory = fileServiceFactory ?? throw new ArgumentNullException(nameof(fileServiceFactory));
    }

    public string? Convert(string? sourceMember, ResolutionContext context)
    {
        if (string.IsNullOrEmpty(sourceMember))
            return null;

        try
        {
            var fileService = _fileServiceFactory.CreateFileService();
            return fileService.GetFileUrl(sourceMember);
        }
        catch
        {
            // If any error occurs, return the original path
            return sourceMember;
        }
    }
}
