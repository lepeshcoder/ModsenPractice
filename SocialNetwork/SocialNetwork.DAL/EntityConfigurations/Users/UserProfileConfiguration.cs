﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetwork.DAL.Entities.Users;

namespace SocialNetwork.DAL.EntityConfigurations.Users;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");
        
        builder.HasKey(e => e.Id).HasName("PRIMARY");
        
        builder.HasIndex(e => e.UserId, "FK_user_profiles_users_idx");

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd()
            .IsRequired();
        builder.Property(e => e.UserName)
            .HasColumnName("user_name")
            .HasMaxLength(Constants.UserNameMaxLength);
        builder.Property(e => e.UserSurname)
            .HasColumnName("user_surname")
            .HasMaxLength(Constants.UserSurnameMaxLength);
        builder.Property(e => e.UserSex)
            .HasColumnName("user_sex")
            .HasMaxLength(Constants.UserSexMaxLength);
        builder.Property(e => e.UserCountry)
            .HasColumnName("user_country")
            .HasMaxLength(Constants.CountryNameMaxLength);
        builder.Property(e => e.UserEducation)
            .HasColumnName("user_education")
            .HasMaxLength(Constants.UserEducationMaxLength);
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime")
            .IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime");
        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();
        builder.Property(e => e.ProfilePictureId)
            .HasColumnName("profile_picture_id");
    
        builder.HasOne(up => up.User).WithOne(u => u.UserProfile)
            .HasForeignKey<UserProfile>(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_user_profiles_users");

        builder.Property(e => e.ProfilePictureId)
    .HasColumnName("profile_picture_id");    

        builder.HasOne(up => up.ProfilePicture).WithMany(u => u.UserProfile)
            .HasForeignKey(up => up.ProfilePictureId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_picture_profiles_picture");
    }
}
