using AutoMapper;
using SocialNetwork.BLL.Contracts;
using SocialNetwork.BLL.DTO.Chats.Request;
using SocialNetwork.BLL.DTO.Chats.Response;
using SocialNetwork.BLL.DTO.Communities.Request;
using SocialNetwork.BLL.DTO.Communities.Response;
using SocialNetwork.BLL.DTO.Medias.Response;
using SocialNetwork.BLL.DTO.Messages.Request;
using SocialNetwork.BLL.DTO.Messages.Response;
using SocialNetwork.BLL.Exceptions;
using SocialNetwork.DAL.Contracts.Chats;
using SocialNetwork.DAL.Contracts.Medias;
using SocialNetwork.DAL.Contracts.Messages;
using SocialNetwork.DAL.Entities.Chats;
using SocialNetwork.DAL.Entities.Communities;
using SocialNetwork.DAL.Entities.Messages;

namespace SocialNetwork.BLL.Services;

public class ChatService : IChatService
{
    private readonly IMapper _mapper;
    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageMediaRepository _messageMediaRepository;
    private readonly IChatMemberRepository _chatMemberRepository;
    private readonly IMediaRepository _mediaRepository;

    public ChatService(
        IMapper mapper,
        IChatRepository chatRepository,
        IChatMemberRepository chatMemberRepository,
        IMessageRepository messageRepository,
        IMessageMediaRepository messageMediaRepository,
        IMediaRepository mediaRepository)
    {
        _mapper = mapper;
        _mediaRepository = mediaRepository;
        _chatRepository = chatRepository;
        _chatMemberRepository = chatMemberRepository;
        _messageRepository = messageRepository;
        _messageMediaRepository = messageMediaRepository;
    }

    public async Task<ChatResponseDto?> GetChatById(uint chatId)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId);
        var chatResponseDto = _mapper.Map<ChatResponseDto>(chat);
        chatResponseDto.UserCount = (await _chatMemberRepository.GetAllAsync(
            cm => cm.ChatId == chatId)).Count();
        return chatResponseDto;
    }

    public async Task<ChatMember> GetChatOwnerByChatId(uint chatId)
    {
        return (await _chatMemberRepository.GetAsync(
            cm => cm.ChatId == chatId && cm.TypeId == ChatMemberType.Owner))!;
    }

    public async Task DeleteChat(uint chatId)
    {
        await _chatRepository.DeleteById(chatId);
        await _chatRepository.SaveAsync();
    }

    public async Task<bool> IsUserHaveChatAdminPermissions(uint chatId, uint userId)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId);
        var chatMember = chat?.ChatMembers.FirstOrDefault(cm => cm.UserId == userId);
        return chatMember?.TypeId is ChatMemberType.Admin or ChatMemberType.Owner;
    }
    private async Task<ChatMember?> GetChatMember(uint chatId, uint userId)
    {
        return await _chatMemberRepository.GetAsync(m => m.UserId == userId && m.ChatId == chatId);
    }
    public async Task<ChatMemberResponseDto> DeleteChatMember(uint userId, uint userToDeleteId, uint chatId)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId) ??
            throw new NotFoundException("No chat with this Id.");
        var chatMemberToDelete = await GetChatMember(chatId, userToDeleteId);
        if (chatMemberToDelete == null)
            throw new NotFoundException("User is not a chat member.");

        var chatMember = await GetChatMember(chatId, userId);
        if (chatMember == null)
            throw new OwnershipException("Only chat members can delete members from communities.");

        if (userToDeleteId != userId)
        {
            if (chatMember.TypeId == ChatMemberType.Member)
            {
                throw new OwnershipException("Chat members can't delete chat members.");
            }
            else if (chatMember.TypeId == ChatMemberType.Admin && chatMemberToDelete.TypeId == ChatMemberType.Admin)
            {
                throw new OwnershipException("Chat admin can't delete chat admin.");
            }
            else if (chatMember.TypeId == ChatMemberType.Admin && chatMemberToDelete.TypeId == ChatMemberType.Owner)
            {
                throw new OwnershipException("Chat admin can't delete chat owner.");
            }
        }
        else
        {
            if (chatMemberToDelete.TypeId == ChatMemberType.Owner)
            {
                throw new OwnershipException("Owner can't delete himself.");
            }
        }

        _chatMemberRepository.Delete(chatMemberToDelete);
        await _chatMemberRepository.SaveAsync();
        return _mapper.Map<ChatMemberResponseDto>(chatMemberToDelete);
    }

    public async Task<bool> IsUserChatMember(uint chatId, uint userId)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId);
        var chatMember = chat?.ChatMembers.FirstOrDefault(cm => cm.UserId == userId);
        return chatMember != null;
    }

    public async Task<List<ChatMember>> GetAllChatMembers(uint chatId, int limit, int currCursor)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId);
        return chat!.ChatMembers.OrderBy(cm => cm.Id)
            .Skip(currCursor)
            .Take(limit)
            .ToList();
    }

    public async Task<List<Message>> GetAllChatMessages(uint chatId, int limit, int nextCursor)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId);
        return chat!.Messages.OrderBy(m => m.Id)
            .Where(p => p.Id > nextCursor)
            .Take(limit)
            .ToList();
    }
    public async Task<Chat> AddChat(Chat newChat)
    {
        var chat = await _chatRepository.AddAsync(newChat);
        await _chatRepository.SaveAsync();
        return chat;
    }

    public async Task<ChatMember> AddChatMember(ChatMember chatMember)
    {
        var newChatMember = await _chatMemberRepository.AddAsync(chatMember);
        await _chatMemberRepository.SaveAsync();
        return newChatMember;
    }

    public async Task<Message> AddMessage(Message message)
    {
        var newMessage = await _messageRepository.AddAsync(message);
        await _messageRepository.SaveAsync();
        return newMessage;
    }

    public async Task<List<MediaResponseDto>> GetAllChatMedias(uint chatId, int limit, int nextCursor)
    {
        var messageMedias = await _messageMediaRepository.GetAllAsync(messageMedia => messageMedia.ChatId == chatId);

        var mediaIds = messageMedias.Select(messageMedia => messageMedia.MediaId).ToList();

        var mediaList = (await _mediaRepository.GetAllAsync(media => mediaIds.Contains(media.Id))).OrderBy(m => m.Id)
            .Where(p => p.Id > nextCursor)
            .Take(limit)
            .ToList();
        return _mapper.Map<List<MediaResponseDto>>(mediaList);
    }

    public async Task<ChatResponseDto> ChangeChat(uint chatId, ChatPatchRequestDto chatPatchRequestDto)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId);
        bool updated = false;
        if (chatPatchRequestDto.ChatPictureId != null)
        {   
            var media = await _mediaRepository.GetByIdAsync((uint)chatPatchRequestDto.ChatPictureId);
            if (media == null)
                throw new ArgumentException($"Media with id equal {chatPatchRequestDto.ChatPictureId} doesn't exist.");
            else
            {
                if (chat.ChatPictureId != chatPatchRequestDto.ChatPictureId)
                {
                    chat.ChatPictureId = chatPatchRequestDto.ChatPictureId;
                    updated = true;
                }                
            }
        }
        if (chatPatchRequestDto.Name != null)
        {
            if (chatPatchRequestDto.Name.Length == 0)
                throw new ArgumentException($"Chat name should have at east one character.");
            else
            {
                if (chat.Name != chatPatchRequestDto.Name)
                {
                    chat.Name = chatPatchRequestDto.Name;
                    updated = true;
                }                
            }
        }        
        if (updated)
        {
            chat!.UpdatedAt = DateTime.Now;
            _chatRepository.Update(chat!);
            await _chatRepository.SaveAsync();            
        }
        return _mapper.Map<ChatResponseDto>(chat);
    }

    public async Task<ChatResponseDto> CreateChat(ChatRequestDto chatRequestDto, uint userId)
    {
        var newChat = new Chat 
            { 
                Name = chatRequestDto.Name,
                CreatedAt = DateTime.Now
            };
        var addedChat = await AddChat(newChat);

        var chatOwner = new ChatMember
        {
            ChatId = addedChat.Id,
            CreatedAt = DateTime.Now,
            TypeId = ChatMemberType.Owner,
            UserId = userId
        };
        await AddChatMember(chatOwner);
        
        return _mapper.Map<ChatResponseDto>(addedChat);
    }

    public async Task<ChatResponseDto> GetChatInfo(uint chatId, uint userId)
    {
        var chat = await GetChatById(chatId);
        if (chat == null) 
            throw new NotFoundException("Chat with request Id doesn't exist");
        
        var isUserChatMember = await IsUserChatMember(chatId, userId);            
        if (!isUserChatMember) 
            throw new AccessDeniedException("You are not chat member.");

        return _mapper.Map<ChatResponseDto>(chat);
    }

    public async Task<List<MediaResponseDto>> GetChatMedias(uint userId, uint chatId, int limit, int nextCursor)
    {
        var chat = await GetChatById(chatId);
        if (chat == null)
            throw new NotFoundException("Chat with request Id doesn't exist");

        var isChatMember = await IsUserChatMember(chatId, userId);
        if (!isChatMember) 
            throw new AccessDeniedException("You are not chat member.");

        return await GetAllChatMedias(chatId, limit, nextCursor);
    }

    public async Task<ChatResponseDto> UpdateChat(uint chatId, uint userId, ChatPatchRequestDto chatPatchRequestDto)
    {
        var chat = await GetChatById(chatId);
        if (chat == null)
            throw new NotFoundException("Chat with request Id doesn't exist");

        var chatOwner = await GetChatOwnerByChatId(chatId);
        if (chatOwner.UserId != userId) 
            throw new OwnershipException("You are not chat Owner");
        
        var updatedChat = await ChangeChat(chatId, chatPatchRequestDto);

        return updatedChat;
    }

    public async Task<ChatResponseDto> DeleteChat(uint chatId, uint userId)
    {
        var chat = await GetChatById(chatId);
        if (chat == null)
            throw new NotFoundException("Chat with request Id doesn't exist");
        
        var chatOwner = await GetChatOwnerByChatId(chatId);
        if (userId != chatOwner.UserId)
            throw new OwnershipException("You are not chat Owner");
       
        await DeleteChat(chatId);

        return _mapper.Map<ChatResponseDto>(chat);
    }

    public async Task<ChatMemberResponseDto> AddChatMember(uint userId, uint chatId, ChatMemberRequestDto postChatMemberDto)
    {
        var chat = await GetChatById(chatId);
        if (chat == null) 
            throw new NotFoundException("Chat with request Id doesn't exist");
        
        var isUserChatMember = await IsUserChatMember(chatId, userId);
        if (!isUserChatMember)
            throw new AccessDeniedException("User isn't chat member");
        
        var isNewMemberAlreadyInChat = await IsUserChatMember(chatId, postChatMemberDto.UserId); 
        if (isNewMemberAlreadyInChat) 
            throw new DuplicateEntryException("User is already in chat");
        
        var newChatMember = new ChatMember
        {
            ChatId = chatId,
            CreatedAt = DateTime.Now,
            TypeId = ChatMemberType.Member,
            UserId = postChatMemberDto.UserId
        };

        var addedChatMember = await AddChatMember(newChatMember);

        return _mapper.Map<ChatMemberResponseDto>(addedChatMember);
    }

    public async Task<List<ChatMemberResponseDto>> GetChatMembers(uint userId, uint chatId, int limit, int nextCursor)
    {
        var chat = await GetChatById(chatId);
        if (chat == null)
            throw new NotFoundException("Chat with request Id doesn't exist");
        
        var isUserChatMember = await IsUserChatMember(chatId, userId);
        if (!isUserChatMember) 
            throw new AccessDeniedException("User isn't chat member");
   
        var chatMembers = await GetAllChatMembers(chatId, limit, nextCursor);

        return chatMembers.Select(cm => _mapper.Map<ChatMemberResponseDto>(cm)).ToList();
    }

    public async Task<ChatMemberResponseDto> UpdateChatMember(uint chatId, uint userId, uint memberId,
        ChangeChatMemberRequestDto changeChatMemberRequestDto)
    {
        var community = await _chatRepository.GetByIdAsync(chatId) ??
    throw new NotFoundException("No community with this Id.");
        var communityMemberToChange = await GetChatMember(chatId, memberId);
        if (communityMemberToChange == null)
            throw new NotFoundException("User is not a community member.");

        var chatMember = await GetChatMember(chatId, userId);
        if (chatMember == null)
            throw new OwnershipException("Only community members can change members in communities.");

        if (chatMember.TypeId == ChatMemberType.Owner)
        {
            if (memberId == userId)
            {
                throw new OwnershipException("Owner cant change himself.");
            }
        }

        if (chatMember.TypeId == ChatMemberType.Admin)
        {
            if (memberId != userId && changeChatMemberRequestDto.Type != ChatMemberType.Member)
            {
                throw new OwnershipException("Admin can only change himself to user.");
            }
        }

        if (chatMember.TypeId == ChatMemberType.Member)
        {
            throw new OwnershipException("Member can't change anything.");
        }

        communityMemberToChange.UpdatedAt = DateTime.Now;
        communityMemberToChange.TypeId = changeChatMemberRequestDto.Type;
        _chatMemberRepository.Update(communityMemberToChange);
        await _chatMemberRepository.SaveAsync();
        return _mapper.Map<ChatMemberResponseDto>(communityMemberToChange);
    }

    public async Task<MessageResponseDto> SendMessage(uint chatId, uint userId, MessageRequestDto postChatMemberDto)
    {
        var chat = await GetChatById(chatId);
        if (chat == null)
            throw new NotFoundException("Chat with request ID doesn't exist");
        
        var isUserChatMember = await IsUserChatMember(chatId, userId);
        if (!isUserChatMember) 
            throw new AccessDeniedException("User isn't chat member");
        
        var newMessage = new Message
        {
            ChatId = chatId,
            Content = postChatMemberDto.Content,
            CreatedAt = DateTime.Now,
            SenderId = userId
        };

        var addedMessage = await AddMessage(newMessage);

        return _mapper.Map<MessageResponseDto>(addedMessage);
    }

    public async Task<List<MessageResponseDto>> GetChatMessages(uint chatId, uint userId, int limit, int nextCursor)
    {
        var chat = await GetChatById(chatId);
        if (chat == null) 
            throw new NotFoundException("Chat with request Id doesn't exist");
        
        var isUserChatMember = await IsUserChatMember(chatId, userId);
        if (!isUserChatMember) 
            throw new AccessDeniedException("User isn't chat member");
       
        var messages = await GetAllChatMessages(chatId, limit, nextCursor);

        return messages.Select(m => _mapper.Map<MessageResponseDto>(m)).ToList();
    }
}