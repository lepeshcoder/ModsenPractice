using SocialNetwork.DAL.Entities.Chats;
using SocialNetwork.DAL.Entities.Messages;

namespace SocialNetwork.BLL.Contracts;

public interface IChatService
{
    public Task<Chat?> GetChatById(uint chatId);
    public Task<ChatMember> GetChatOwnerByChatId(uint chatId);
    public Task DeleteChat(uint chatId);
    public Task<bool> IsUserHaveChatAdminPermissions(uint chatId, uint userId);
    public Task<ChatMember?> DeleteChatMember(uint chatId, uint userId);
    public Task<bool> IsUserChatMember(uint chatId, uint userId);
    public Task<List<ChatMember>> GetAllChatMembers(uint chatId, int limit, int currCursor);
    Task<List<Message>> GetAllChatMessages(uint chatId, int limit, int nextCursor);
    Task<Chat> AddChat(Chat newChat);
    Task<ChatMember> AddChatMember(ChatMember chatOwner);
    Task<Message> AddMessage(Message message);
}