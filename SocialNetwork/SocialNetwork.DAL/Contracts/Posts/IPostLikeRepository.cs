using SocialNetwork.DAL.Entities.Posts;

namespace SocialNetwork.DAL.Contracts.Posts;

public interface IPostLikeRepository : IRepository<PostLike>
{
    Task<PostLike> LikeComment(uint userId, uint postId);    
}