using AnonymousAPI.Controllers;
using AnonymousDomain.Models.Attachment;
using AttachmentService.Clients;
using AttachmentService.Context;
using AttachmentService.Interfaces;
using AttachmentService.Models.Attachment.DTOs;
using AttachmentService.Models.DTOs;
using AttachmentService.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace AttachmentServiceTest
{
    public class AttachmentRepositoryTests
    {
        private AttachmentContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AttachmentContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(x => x["ConnectionStrings:AttachmentDB"]).Returns("FakeConnectionString");

            return new AttachmentContext(options, configMock.Object);
        }

        [Fact]
        public void GetAll_ReturnsAllAttachments()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var attachments = new List<Attachment>
            {
                new Attachment { Id = Guid.NewGuid(), FileName = "file1.pdf", FileType = "pdf", Url = "http://url1.com", UploadedAt = DateTime.UtcNow },
                new Attachment { Id = Guid.NewGuid(), FileName = "file2.pdf", FileType = "pdf", Url = "http://url2.com", UploadedAt = DateTime.UtcNow }
            };
            context.Attachments.AddRange(attachments);
            context.SaveChanges();

            var attachmentDtos = attachments.Select(a => new AttachmentDTO { Id = a.Id, FileName = a.FileName, FileType = a.FileType, Url = a.Url }).ToList();
            mapper.Setup(m => m.Map<IEnumerable<AttachmentDTO>>(It.IsAny<IEnumerable<Attachment>>())).Returns(attachmentDtos);

            var repo = new AttachmentRepository(context, mapper.Object);

            var result = repo.GetAll();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetAll_ReturnsEmpty_WhenNoAttachments()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<IEnumerable<AttachmentDTO>>(It.IsAny<IEnumerable<Attachment>>())).Returns(new List<AttachmentDTO>());

            var repo = new AttachmentRepository(context, mapper.Object);

            var result = repo.GetAll();

            Assert.Empty(result);
        }

        [Fact]
        public void GetById_ReturnsAttachment_WhenExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var id = Guid.NewGuid();
            var attachment = new Attachment { Id = id, FileName = "file.pdf", FileType = "pdf", Url = "http://url.com", UploadedAt = DateTime.UtcNow };
            context.Attachments.Add(attachment);
            context.SaveChanges();

            var attachmentDto = new AttachmentDTO { Id = id, FileName = "file.pdf", FileType = "pdf", Url = "http://url.com" };
            mapper.Setup(m => m.Map<AttachmentDTO>(It.IsAny<Attachment>())).Returns(attachmentDto);

            var repo = new AttachmentRepository(context, mapper.Object);

            var result = repo.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public void GetById_ReturnsNull_WhenNotExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<AttachmentDTO>(null)).Returns((AttachmentDTO)null);

            var repo = new AttachmentRepository(context, mapper.Object);

            var result = repo.GetById(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public void GetBySuggestionId_ReturnsAttachments_WhenExist()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var suggestionId = Guid.NewGuid();
            var attachments = new List<Attachment>
            {
                new Attachment { Id = Guid.NewGuid(), FileName = "file1.pdf", FileType = "pdf", Url = "http://url1.com", UploadedAt = DateTime.UtcNow, SuggestionId = suggestionId },
                new Attachment { Id = Guid.NewGuid(), FileName = "file2.pdf", FileType = "pdf", Url = "http://url2.com", UploadedAt = DateTime.UtcNow, SuggestionId = suggestionId }
            };
            context.Attachments.AddRange(attachments);
            context.SaveChanges();

            var attachmentDtos = attachments.Select(a => new AttachmentDTO { Id = a.Id, FileName = a.FileName, SuggestionId = suggestionId }).ToList();
            mapper.Setup(m => m.Map<IEnumerable<AttachmentDTO>>(It.IsAny<IEnumerable<Attachment>>())).Returns(attachmentDtos);

            var repo = new AttachmentRepository(context, mapper.Object);

            var result = repo.GetBySuggestionId(suggestionId);

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetBySuggestionId_ReturnsEmpty_WhenNoAttachments()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<IEnumerable<AttachmentDTO>>(It.IsAny<IEnumerable<Attachment>>())).Returns(new List<AttachmentDTO>());

            var repo = new AttachmentRepository(context, mapper.Object);

            var result = repo.GetBySuggestionId(Guid.NewGuid());

            Assert.Empty(result);
        }

        [Fact]
        public void GetByProblemId_ReturnsAttachments_WhenExist()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var problemId = Guid.NewGuid();
            var attachments = new List<Attachment>
            {
                new Attachment { Id = Guid.NewGuid(), FileName = "file1.pdf", FileType = "pdf", Url = "http://url1.com", UploadedAt = DateTime.UtcNow, ProblemId = problemId },
                new Attachment { Id = Guid.NewGuid(), FileName = "file2.pdf", FileType = "pdf", Url = "http://url2.com", UploadedAt = DateTime.UtcNow, ProblemId = problemId }
            };
            context.Attachments.AddRange(attachments);
            context.SaveChanges();

            var attachmentDtos = attachments.Select(a => new AttachmentDTO { Id = a.Id, FileName = a.FileName, ProblemId = problemId }).ToList();
            mapper.Setup(m => m.Map<IEnumerable<AttachmentDTO>>(It.IsAny<IEnumerable<Attachment>>())).Returns(attachmentDtos);

            var repo = new AttachmentRepository(context, mapper.Object);

            var result = repo.GetByProblemId(problemId);

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetByProblemId_ReturnsEmpty_WhenNoAttachments()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<IEnumerable<AttachmentDTO>>(It.IsAny<IEnumerable<Attachment>>())).Returns(new List<AttachmentDTO>());

            var repo = new AttachmentRepository(context, mapper.Object);

            var result = repo.GetByProblemId(Guid.NewGuid());

            Assert.Empty(result);
        }

        [Fact]
        public void Create_AddsAttachment_AndReturnsDTO()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var creationDto = new AttachmentCreationDTO
            {
                FileName = "file.pdf",
                FileType = "pdf",
                Url = "http://url.com",
                SuggestionId = Guid.NewGuid()
            };
            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                FileName = creationDto.FileName,
                FileType = creationDto.FileType,
                Url = creationDto.Url,
                UploadedAt = DateTime.UtcNow,
                SuggestionId = creationDto.SuggestionId
            };
            var attachmentDto = new AttachmentDTO { Id = attachment.Id, FileName = attachment.FileName };

            mapper.Setup(m => m.Map<Attachment>(creationDto)).Returns(attachment);
            mapper.Setup(m => m.Map<AttachmentDTO>(attachment)).Returns(attachmentDto);

            var repo = new AttachmentRepository(context, mapper.Object);

            var result = repo.Create(creationDto);

            Assert.NotNull(result);
            Assert.Equal("file.pdf", result.FileName);
            Assert.Equal(1, context.Attachments.Count());
        }

        [Fact]
        public void Update_UpdatesAttachment_AndReturnsDTO()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var id = Guid.NewGuid();
            var attachment = new Attachment { Id = id, FileName = "old.pdf", FileType = "pdf", Url = "http://old.com", UploadedAt = DateTime.UtcNow };
            context.Attachments.Add(attachment);
            context.SaveChanges();

            var updateDto = new AttachmentUpdateDTO { Id = id, FileName = "updated.pdf", FileType = "pdf", Url = "http://updated.com" };
            var updatedDto = new AttachmentDTO { Id = id, FileName = "updated.pdf" };

            mapper.Setup(m => m.Map(updateDto, It.IsAny<Attachment>())).Callback<AttachmentUpdateDTO, Attachment>((dto, a) =>
            {
                a.FileName = dto.FileName;
                a.Url = dto.Url;
            });
            mapper.Setup(m => m.Map<AttachmentDTO>(It.IsAny<Attachment>())).Returns(updatedDto);

            var repo = new AttachmentRepository(context, mapper.Object);

            var result = repo.Update(updateDto);

            Assert.NotNull(result);
            Assert.Equal("updated.pdf", result.FileName);
        }

        [Fact]
        public void Delete_RemovesAttachment_WhenExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var id = Guid.NewGuid();
            var attachment = new Attachment { Id = id, FileName = "file.pdf", FileType = "pdf", Url = "http://url.com", UploadedAt = DateTime.UtcNow };
            context.Attachments.Add(attachment);
            context.SaveChanges();

            var repo = new AttachmentRepository(context, mapper.Object);

            repo.Delete(id);

            Assert.Equal(0, context.Attachments.Count());
        }

        [Fact]
        public void Delete_DoesNotThrow_WhenAttachmentNotExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var repo = new AttachmentRepository(context, mapper.Object);

            var exception = Record.Exception(() => repo.Delete(Guid.NewGuid()));

            Assert.Null(exception);
        }

        [Fact]
        public void SaveChanges_ReturnsTrue_WhenChangesSaved()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var attachment = new Attachment { Id = Guid.NewGuid(), FileName = "file.pdf", FileType = "pdf", Url = "http://url.com", UploadedAt = DateTime.UtcNow };
            context.Attachments.Add(attachment);

            var repo = new AttachmentRepository(context, mapper.Object);

            var result = repo.SaveChanges();

            Assert.True(result);
        }
    }

    // ==================== ATTACHMENT CONTROLLER TESTS ====================
    public class AttachmentControllerTests
    {
        private readonly Mock<IAttachmentRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly AttachmentController _controller;
        private readonly Mock<LoggerServiceClient> _logger;

        public AttachmentControllerTests()
        {
            _mockRepo = new Mock<IAttachmentRepository>();
            _mapper = Mock.Of<IMapper>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new AttachmentController(_mockRepo.Object, _mapper, _logger.Object);
        }

        [Fact]
        public void GetAllAttachments_ReturnsOkResult()
        {
            var attachments = new List<AttachmentDTO>
            {
                new AttachmentDTO { Id = Guid.NewGuid(), FileName = "file1.pdf" },
                new AttachmentDTO { Id = Guid.NewGuid(), FileName = "file2.pdf" }
            };
            _mockRepo.Setup(repo => repo.GetAll()).Returns(attachments);

            var result = _controller.GetAllAttachments();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<AttachmentDTO>>(okResult.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public void GetAttachmentBySuggestionId_ReturnsOkResult()
        {
            var suggestionId = Guid.NewGuid();
            var attachments = new List<AttachmentDTO>
            {
                new AttachmentDTO { Id = Guid.NewGuid(), SuggestionId = suggestionId }
            };
            _mockRepo.Setup(repo => repo.GetBySuggestionId(suggestionId)).Returns(attachments);

            var result = _controller.GetAttachmentBySuggestionId(suggestionId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<AttachmentDTO>>(okResult.Value);
            Assert.Single(returned);
        }

        [Fact]
        public void GetAttachmentByProblemId_ReturnsOkResult()
        {
            var problemId = Guid.NewGuid();
            var attachments = new List<AttachmentDTO>
            {
                new AttachmentDTO { Id = Guid.NewGuid(), ProblemId = problemId }
            };
            _mockRepo.Setup(repo => repo.GetByProblemId(problemId)).Returns(attachments);

            var result = _controller.GetAttachmentByProblemId(problemId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<AttachmentDTO>>(okResult.Value);
            Assert.Single(returned);
        }
    }
}