using SocialNetwork.BLL.DTO.Messages.Response;
using SocialNetwork.DAL.Entities.Medias;
using SocialNetwork.DAL.Entities.Messages;

namespace SocialNetwork.BLL.AutoMapperProfiles;

public class MessagesProfile: BaseProfile
{
    public MessagesProfile()
    {
        CreateMap<MessageLike, MessageLikeResponseDto>();
        CreateMap<Message, MessageResponseDto>().ForMember(
            dto => dto.LikeCount,
            expression => expression.MapFrom(message => message.MessageLikes.Count)).
            ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments != null ? GetBobaList(src.Attachments) : null));

    }
    private List<Media> GetBobaList(ICollection<MessageMedia> messageMedias)
    {
        List<Media> medias = new List<Media>();
        foreach (var b in messageMedias)
        {
            medias.Add(b.Media);
        }
        return medias;
    }
}