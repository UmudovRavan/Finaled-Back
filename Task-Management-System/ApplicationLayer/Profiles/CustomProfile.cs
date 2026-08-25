using AutoMapper;
using Contract.DTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Profiles
{
    public class CustomProfile : Profile
    {
        public CustomProfile()
        {
            // File mapping: DTO -> Entity
            CreateMap<FileDto, TaskAttachment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Task, opt => opt.Ignore());

            // File mapping: Entity -> DTO
            CreateMap<TaskAttachment, FileDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
                .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.ContentType))
                .ForMember(dest => dest.Size, opt => opt.MapFrom(src => src.Size))
                .ForMember(dest => dest.Url, opt => opt.MapFrom(src => $"https://api-tms.altensor.com/api/TaskAttachment/{src.Id}/preview"))
                .ForMember(dest => dest.Content, opt => opt.Ignore());

            // Task mapping: DTO -> Entity
            CreateMap<TaskDTO, TaskItem>()
                .ForMember(dest => dest.Attachments, opt => opt.Ignore())
                .ForMember(dest => dest.TaskComments, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedToUser, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedToUserId, opt => opt.MapFrom(src => SafeParseGuid(src.AssignedToUserId)))
                .ForMember(dest => dest.CreatedByUserId, opt => opt.MapFrom(src => SafeParseGuid(src.CreatedByUserId) ?? Guid.Empty))
                .ForMember(dest => dest.AssignedWorkGroupId, opt => opt.MapFrom(src => src.WorkGroupId));

            // Task mapping: Entity -> DTO
            CreateMap<TaskItem, TaskDTO>()
                .ForMember(dest => dest.Files, opt => opt.MapFrom(src => src.Attachments))
                .ForMember(dest => dest.AssignedToUserId, opt => opt.MapFrom(src => src.AssignedToUserId != null ? src.AssignedToUserId.ToString() : null))
                .ForMember(dest => dest.CreatedByUserId, opt => opt.MapFrom(src => src.CreatedByUserId.ToString()))
                .ForMember(dest => dest.WorkGroupId, opt => opt.MapFrom(src => src.AssignedWorkGroupId))
                .ForMember(dest => dest.TaskCommentId, opt => opt.MapFrom(src => src.TaskComments != null
                    ? src.TaskComments.Select(c => c.Id).ToList()
                    : null))
                .ForMember(dest => dest.TaskComments, opt => opt.MapFrom(src => src.TaskComments));

            CreateMap<TaskComment, TaskCommentDTO>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId.ToString()))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.UserName : null))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.TaskCommentMentionIDs, opt => opt.MapFrom(src =>
                    src.TaskCommentMentions != null
                        ? src.TaskCommentMentions.Select(m => m.MentionedUserId.ToString()).ToList()
                        : null))
                .ReverseMap()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => Guid.Parse(src.UserId)))
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.TaskItem, opt => opt.Ignore())
                .ForMember(dest => dest.TaskCommentMentions, opt => opt.Ignore());

            CreateMap<WorkGroup, WorkGroupDTO>()
                .ForMember(dest => dest.LeaderId, opt => opt.MapFrom(src => src.LeaderId.ToString()))
                .ForMember(dest => dest.UserIds, opt => opt.MapFrom(src => src.Users.Select(u => u.Id.ToString()).ToList()))
                .ForMember(dest => dest.TaskIds, opt => opt.MapFrom(src => src.Tasks != null ? src.Tasks.Select(t => new TaskDTO
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    AssignedToUserId = t.AssignedToUserId != null ? t.AssignedToUserId.ToString() : null,
                    CreatedByUserId = t.CreatedByUserId.ToString(),
                    Deadline = t.Deadline,
                    Status = t.Status,
                    Difficulty = t.Difficulty,
                    WorkGroupId = t.AssignedWorkGroupId
                }).ToList() : new List<TaskDTO>()));

            CreateMap<WorkGroupDTO, WorkGroup>()
                .ForMember(dest => dest.Users, opt => opt.Ignore())
                .ForMember(dest => dest.Tasks, opt => opt.Ignore())
                .ForMember(dest => dest.LeaderId, opt => opt.MapFrom(src => SafeParseGuid(src.LeaderId) ?? Guid.Empty));
        }

        private static Guid? SafeParseGuid(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "null" || value == "undefined")
                return null;
            return Guid.TryParse(value, out var result) ? result : null;
        }
    }
}
