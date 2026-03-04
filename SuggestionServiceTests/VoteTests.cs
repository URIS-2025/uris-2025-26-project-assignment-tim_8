using AnonymousDomain.Models.Suggestion;
using AnonymousRepository.Repositories;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SuggestionService.Models.DTOs;
using Xunit;

namespace SuggestionServiceTest
{
    public class VoteRepositoryTests
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
        public void GetAll_ReturnsAllVotes()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var votes = new List<Vote>
            {
                new Vote { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, VoteAuthorId = Guid.NewGuid(), SuggestionId = Guid.NewGuid() },
                new Vote { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, VoteAuthorId = Guid.NewGuid(), SuggestionId = Guid.NewGuid() }
            };
            context.Votes.AddRange(votes);
            context.SaveChanges();

            var voteDtos = votes.Select(v => new VoteDTO { Id = v.Id, VoteAuthorId = v.VoteAuthorId, SuggestionId = v.SuggestionId }).ToList();
            mapper.Setup(m => m.Map<IEnumerable<VoteDTO>>(It.IsAny<IEnumerable<Vote>>())).Returns(voteDtos);

            var repo = new VoteRepository(context, mapper.Object);

            var result = repo.GetAll();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetAll_ReturnsEmpty_WhenNoVotes()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<IEnumerable<VoteDTO>>(It.IsAny<IEnumerable<Vote>>())).Returns(new List<VoteDTO>());

            var repo = new VoteRepository(context, mapper.Object);

            var result = repo.GetAll();

            Assert.Empty(result);
        }

        [Fact]
        public void GetById_ReturnsVote_WhenExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var id = Guid.NewGuid();
            var vote = new Vote { Id = id, CreatedAt = DateTime.UtcNow, VoteAuthorId = Guid.NewGuid(), SuggestionId = Guid.NewGuid() };
            context.Votes.Add(vote);
            context.SaveChanges();

            var voteDto = new VoteDTO { Id = id, VoteAuthorId = vote.VoteAuthorId, SuggestionId = vote.SuggestionId };
            mapper.Setup(m => m.Map<VoteDTO>(It.IsAny<Vote>())).Returns(voteDto);

            var repo = new VoteRepository(context, mapper.Object);

            var result = repo.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public void GetById_ReturnsNull_WhenNotExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<VoteDTO>(null)).Returns((VoteDTO)null);

            var repo = new VoteRepository(context, mapper.Object);

            var result = repo.GetById(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public void GetBySuggestionId_ReturnsVotes_WhenExist()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var suggestionId = Guid.NewGuid();
            var votes = new List<Vote>
            {
                new Vote { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, VoteAuthorId = Guid.NewGuid(), SuggestionId = suggestionId },
                new Vote { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, VoteAuthorId = Guid.NewGuid(), SuggestionId = suggestionId }
            };
            context.Votes.AddRange(votes);
            context.SaveChanges();

            var voteDtos = votes.Select(v => new VoteDTO { Id = v.Id, VoteAuthorId = v.VoteAuthorId, SuggestionId = suggestionId }).ToList();
            mapper.Setup(m => m.Map<IEnumerable<VoteDTO>>(It.IsAny<IEnumerable<Vote>>())).Returns(voteDtos);

            var repo = new VoteRepository(context, mapper.Object);

            var result = repo.GetBySuggestionId(suggestionId);

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetBySuggestionId_ReturnsEmpty_WhenNoVotes()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<IEnumerable<VoteDTO>>(It.IsAny<IEnumerable<Vote>>())).Returns(new List<VoteDTO>());

            var repo = new VoteRepository(context, mapper.Object);

            var result = repo.GetBySuggestionId(Guid.NewGuid());

            Assert.Empty(result);
        }

        [Fact]
        public void Create_AddsVote_AndReturnsDTO()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var creationDto = new VoteCreationDTO
            {
                VoteAuthorId = Guid.NewGuid(),
                SuggestionId = Guid.NewGuid()
            };
            var vote = new Vote
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                VoteAuthorId = creationDto.VoteAuthorId,
                SuggestionId = creationDto.SuggestionId
            };

            mapper.Setup(m => m.Map<Vote>(creationDto)).Returns(vote);
            mapper.Setup(m => m.Map<VoteCreationDTO>(vote)).Returns(creationDto);

            var repo = new VoteRepository(context, mapper.Object);

            var result = repo.Create(creationDto);

            Assert.NotNull(result);
            Assert.Equal(creationDto.VoteAuthorId, result.VoteAuthorId);
            Assert.Equal(1, context.Votes.Count());
        }

        [Fact]
        public void Delete_RemovesVote_WhenExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var id = Guid.NewGuid();
            var vote = new Vote { Id = id, CreatedAt = DateTime.UtcNow, VoteAuthorId = Guid.NewGuid(), SuggestionId = Guid.NewGuid() };
            context.Votes.Add(vote);
            context.SaveChanges();

            var repo = new VoteRepository(context, mapper.Object);

            repo.Delete(id);

            Assert.Equal(0, context.Votes.Count());
        }

        [Fact]
        public void Delete_DoesNotThrow_WhenVoteNotExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var repo = new VoteRepository(context, mapper.Object);

            var exception = Record.Exception(() => repo.Delete(Guid.NewGuid()));

            Assert.Null(exception);
        }

        [Fact]
        public void SaveChanges_ReturnsTrue_WhenChangesSaved()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var vote = new Vote { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, VoteAuthorId = Guid.NewGuid(), SuggestionId = Guid.NewGuid() };
            context.Votes.Add(vote);

            var repo = new VoteRepository(context, mapper.Object);

            var result = repo.SaveChanges();

            Assert.True(result);
        }
    }
}