using AnonymousDomain.Models.Suggestion;
using AnonymousDomain.Models.Suggestion.DTOs;
using AnonymousRepository.Repositories;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SuggestionService.Models.DTOs;
using Xunit;

namespace SuggestionServiceTest
{
    public class SuggestionCommentRepositoryTests
    {
        private SuggestionContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<SuggestionContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(x => x["ConnectionStrings:SuggestionDB"]).Returns("FakeConnectionString");

            return new SuggestionContext(options, configMock.Object);
        }

        [Fact]
        public void GetAll_ReturnsAllComments()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var comments = new List<SuggestionComment>
            {
                new SuggestionComment { Id = Guid.NewGuid(), Text = "Comment 1", IsAnonymous = false, CreatedAt = DateTime.UtcNow, CommentAuthorId = Guid.NewGuid(), CreatedBy = "TestUser" },
                new SuggestionComment { Id = Guid.NewGuid(), Text = "Comment 2", IsAnonymous = true, CreatedAt = DateTime.UtcNow, CommentAuthorId = Guid.NewGuid(), CreatedBy = "TestUser" }
            };
            context.SuggestionComments.AddRange(comments);
            context.SaveChanges();

            var commentDtos = comments.Select(c => new SuggestionCommentDTO { Id = c.Id, Text = c.Text, IsAnonymous = c.IsAnonymous }).ToList();
            mapper.Setup(m => m.Map<IEnumerable<SuggestionCommentDTO>>(It.IsAny<IEnumerable<SuggestionComment>>())).Returns(commentDtos);

            var repo = new SuggestionCommentRepository(context, mapper.Object);

            var result = repo.GetAll();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetAll_ReturnsEmpty_WhenNoComments()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<IEnumerable<SuggestionCommentDTO>>(It.IsAny<IEnumerable<SuggestionComment>>())).Returns(new List<SuggestionCommentDTO>());

            var repo = new SuggestionCommentRepository(context, mapper.Object);

            var result = repo.GetAll();

            Assert.Empty(result);
        }

        [Fact]
        public void GetById_ReturnsComment_WhenExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var id = Guid.NewGuid();
            var comment = new SuggestionComment { Id = id, Text = "Test Comment", IsAnonymous = false, CreatedAt = DateTime.UtcNow, CommentAuthorId = Guid.NewGuid(), CreatedBy = "TestUser" };
            context.SuggestionComments.Add(comment);
            context.SaveChanges();

            var commentDto = new SuggestionCommentDTO { Id = id, Text = "Test Comment", IsAnonymous = false };
            mapper.Setup(m => m.Map<SuggestionCommentDTO>(It.IsAny<SuggestionComment>())).Returns(commentDto);

            var repo = new SuggestionCommentRepository(context, mapper.Object);

            var result = repo.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public void GetById_ReturnsNull_WhenNotExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<SuggestionCommentDTO>(null)).Returns((SuggestionCommentDTO)null);

            var repo = new SuggestionCommentRepository(context, mapper.Object);

            var result = repo.GetById(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public void GetBySuggestionId_ReturnsComments_WhenExist()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var suggestionId = Guid.NewGuid();
            var comments = new List<SuggestionComment>
            {
                new SuggestionComment { Id = Guid.NewGuid(), Text = "Comment 1", IsAnonymous = false, CreatedAt = DateTime.UtcNow, CommentAuthorId = Guid.NewGuid(), CreatedBy = "TestUser", SuggestionId = suggestionId },
                new SuggestionComment { Id = Guid.NewGuid(), Text = "Comment 2", IsAnonymous = true, CreatedAt = DateTime.UtcNow, CommentAuthorId = Guid.NewGuid(), CreatedBy = "TestUser", SuggestionId = suggestionId }
            };
            context.SuggestionComments.AddRange(comments);
            context.SaveChanges();

            var commentDtos = comments.Select(c => new SuggestionCommentDTO { Id = c.Id, Text = c.Text, SuggestionId = suggestionId }).ToList();
            mapper.Setup(m => m.Map<IEnumerable<SuggestionCommentDTO>>(It.IsAny<IEnumerable<SuggestionComment>>())).Returns(commentDtos);

            var repo = new SuggestionCommentRepository(context, mapper.Object);

            var result = repo.GetBySuggestionId(suggestionId);

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetBySuggestionId_ReturnsEmpty_WhenNoComments()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<IEnumerable<SuggestionCommentDTO>>(It.IsAny<IEnumerable<SuggestionComment>>())).Returns(new List<SuggestionCommentDTO>());

            var repo = new SuggestionCommentRepository(context, mapper.Object);

            var result = repo.GetBySuggestionId(Guid.NewGuid());

            Assert.Empty(result);
        }

        [Fact]
        public void Create_AddsComment_AndReturnsDTO()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var creationDto = new SuggestionCommentCreationDTO
            {
                Text = "New Comment",
                IsAnonymous = false,
                SuggestionId = Guid.NewGuid(),
                CommentAuthorId = Guid.NewGuid()
            };
            var comment = new SuggestionComment
            {
                Id = Guid.NewGuid(),
                Text = creationDto.Text,
                IsAnonymous = creationDto.IsAnonymous,
                CreatedAt = DateTime.UtcNow,
                CommentAuthorId = creationDto.CommentAuthorId,
                SuggestionId = creationDto.SuggestionId,
                CreatedBy = "TestUser"
            };

            mapper.Setup(m => m.Map<SuggestionComment>(creationDto)).Returns(comment);
            mapper.Setup(m => m.Map<SuggestionCommentCreationDTO>(comment)).Returns(creationDto);

            var repo = new SuggestionCommentRepository(context, mapper.Object);

            var result = repo.Create(creationDto);

            Assert.NotNull(result);
            Assert.Equal("New Comment", result.Text);
            Assert.Equal(1, context.SuggestionComments.Count());
        }

        [Fact]
        public void Update_UpdatesComment_AndReturnsDTO()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var id = Guid.NewGuid();
            var comment = new SuggestionComment { Id = id, Text = "Old Text", IsAnonymous = false, CreatedAt = DateTime.UtcNow, CommentAuthorId = Guid.NewGuid(), CreatedBy = "TestUser" };
            context.SuggestionComments.Add(comment);
            context.SaveChanges();

            var updateDto = new SuggestionCommentUpdateDTO { Id = id, Text = "Updated Text" };

            mapper.Setup(m => m.Map(updateDto, It.IsAny<SuggestionComment>())).Callback<SuggestionCommentUpdateDTO, SuggestionComment>((dto, c) =>
            {
                c.Text = dto.Text;
            });
            mapper.Setup(m => m.Map<SuggestionCommentUpdateDTO>(It.IsAny<SuggestionComment>())).Returns(updateDto);

            var repo = new SuggestionCommentRepository(context, mapper.Object);

            var result = repo.Update(updateDto);

            Assert.NotNull(result);
            Assert.Equal("Updated Text", result.Text);
        }

        [Fact]
        public void Delete_RemovesComment_WhenExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var id = Guid.NewGuid();
            var comment = new SuggestionComment { Id = id, Text = "Comment", IsAnonymous = false, CreatedAt = DateTime.UtcNow, CommentAuthorId = Guid.NewGuid(), CreatedBy = "TestUser" };
            context.SuggestionComments.Add(comment);
            context.SaveChanges();

            var repo = new SuggestionCommentRepository(context, mapper.Object);

            repo.Delete(id);

            Assert.Equal(0, context.SuggestionComments.Count());
        }

        [Fact]
        public void Delete_DoesNotThrow_WhenCommentNotExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var repo = new SuggestionCommentRepository(context, mapper.Object);

            var exception = Record.Exception(() => repo.Delete(Guid.NewGuid()));

            Assert.Null(exception);
        }

        [Fact]
        public void SaveChanges_ReturnsTrue_WhenChangesSaved()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var comment = new SuggestionComment { Id = Guid.NewGuid(), Text = "Comment", IsAnonymous = false, CreatedAt = DateTime.UtcNow, CommentAuthorId = Guid.NewGuid(), CreatedBy = "TestUser" };
            context.SuggestionComments.Add(comment);

            var repo = new SuggestionCommentRepository(context, mapper.Object);

            var result = repo.SaveChanges();

            Assert.True(result);
        }
    }
}