using AnonymousDomain.Enums;
using AnonymousDomain.Models.Suggestion;
using AnonymousDomain.Models.Suggestion.DTOs;
using AnonymousAPI.Controllers;
using AnonymousRepository.Interfaces;
using AnonymousRepository.Profiles;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SuggestionService.Data;
using SuggestionService.Models.DTOs;
using Xunit;
using SuggestionService.Clients;

namespace SuggestionServiceTest
{
    public class SuggestionControllerTests
    {
        private readonly Mock<ISuggestionRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly SuggestionController _controller;
        private readonly Mock<LoggerServiceClient> _logger;
        private readonly Mock<SuggestionBoxServiceClient> _boxClient;
        public SuggestionControllerTests()
        {
            _mockRepo = new Mock<ISuggestionRepository>();
            _mapper = Mock.Of<IMapper>();
            _logger = new Mock<LoggerServiceClient>();
            _boxClient = new Mock<SuggestionBoxServiceClient>();
            _boxClient
                .Setup(c => c.IsBoxActiveAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _controller = new SuggestionController(_mockRepo.Object, _mapper, _logger.Object, _boxClient.Object);
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        }

        [Fact]
        public async Task CreateSuggestion_ReturnsCreated_WhenBoxIsActive()
        {
            var dto = new SuggestionCreationDTO
            {
                Title = "New Suggestion",
                Description = "Desc",
                SuggestionBoxId = Guid.NewGuid(),
                AnonymousUserId = Guid.NewGuid(),
                CategoryIds = new List<Guid>()
            };
            var created = new SuggestionCreatedDTO { Id = Guid.NewGuid(), Title = "New Suggestion" };
            _mockRepo.Setup(repo => repo.Create(dto)).Returns(created);

            var result = await _controller.CreateSuggestion(dto);

            Assert.IsType<CreatedResult>(result.Result);
            _mockRepo.Verify(repo => repo.Create(It.IsAny<SuggestionCreationDTO>()), Times.Once);
        }

        [Fact]
        public async Task CreateSuggestion_ReturnsConflict_WhenBoxIsNotActive()
        {
            var dto = new SuggestionCreationDTO
            {
                Title = "New Suggestion",
                Description = "Desc",
                SuggestionBoxId = Guid.NewGuid(),
                AnonymousUserId = Guid.NewGuid(),
                CategoryIds = new List<Guid>()
            };
            _boxClient
                .Setup(c => c.IsBoxActiveAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.CreateSuggestion(dto);

            Assert.IsType<ConflictObjectResult>(result.Result);
            _mockRepo.Verify(repo => repo.Create(It.IsAny<SuggestionCreationDTO>()), Times.Never);
        }

        [Fact]
        public void GetAllSuggestions_ReturnsOkResult()
        {
            var suggestions = new List<SuggestionDTO>
            {
                new SuggestionDTO { Id = Guid.NewGuid(), Title = "Test Suggestion 1" },
                new SuggestionDTO { Id = Guid.NewGuid(), Title = "Test Suggestion 2" }
            };
            _mockRepo.Setup(repo => repo.GetAll()).Returns(suggestions);

            var result = _controller.GetAllSuggestions();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedSuggestions = Assert.IsAssignableFrom<IEnumerable<SuggestionDTO>>(okResult.Value);
            Assert.Equal(2, returnedSuggestions.Count());
        }

        [Fact]
        public void GetAllSuggestions_ReturnsEmptyList_WhenNoSuggestions()
        {
            _mockRepo.Setup(repo => repo.GetAll()).Returns(new List<SuggestionDTO>());

            var result = _controller.GetAllSuggestions();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedSuggestions = Assert.IsAssignableFrom<IEnumerable<SuggestionDTO>>(okResult.Value);
            Assert.Empty(returnedSuggestions);
        }

        [Fact]
        public void GetSuggestionById_ReturnsOkResult_WhenSuggestionExists()
        {
            var id = Guid.NewGuid();
            var suggestion = new SuggestionDTO { Id = id, Title = "Test Suggestion" };
            _mockRepo.Setup(repo => repo.GetById(id)).Returns(suggestion);

            var result = _controller.GetSuggestionById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedSuggestion = Assert.IsType<SuggestionDTO>(okResult.Value);
            Assert.Equal(id, returnedSuggestion.Id);
        }

        [Fact]
        public void GetSuggestionById_ReturnsNull_WhenSuggestionDoesNotExist()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.GetById(id)).Returns((SuggestionDTO)null);

            var result = _controller.GetSuggestionById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(okResult.Value);
        }

        [Fact]
        public void GetSuggestionsByUserId_ReturnsOkResult()
        {
            var userId = Guid.NewGuid();
            var suggestions = new List<SuggestionDTO>
            {
                new SuggestionDTO { Id = Guid.NewGuid(), AnonymousUserId = userId }
            };
            _mockRepo.Setup(repo => repo.GetByUserId(userId)).Returns(suggestions);

            var result = _controller.GetSuggestionsByUserId(userId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedSuggestions = Assert.IsAssignableFrom<IEnumerable<SuggestionDTO>>(okResult.Value);
            Assert.Single(returnedSuggestions);
        }
    }

    public class SuggestionCommentControllerTests
    {
        private readonly Mock<ISuggestionCommentRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly SuggestionCommentController _controller;
        private readonly Mock<LoggerServiceClient> _logger;

        public SuggestionCommentControllerTests()
        {
            _mockRepo = new Mock<ISuggestionCommentRepository>();
            _mapper = Mock.Of<IMapper>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new SuggestionCommentController(_mockRepo.Object, _mapper, _logger.Object);
        }

        [Fact]
        public void GetAllSuggestionComments_ReturnsOkResult()
        {
            var comments = new List<SuggestionCommentDTO>
            {
                new SuggestionCommentDTO { Id = Guid.NewGuid(), Text = "Comment 1" },
                new SuggestionCommentDTO { Id = Guid.NewGuid(), Text = "Comment 2" }
            };
            _mockRepo.Setup(repo => repo.GetAll()).Returns(comments);

            var result = _controller.GetAllSuggestionComments();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedComments = Assert.IsAssignableFrom<IEnumerable<SuggestionCommentDTO>>(okResult.Value);
            Assert.Equal(2, returnedComments.Count());
        }

        [Fact]
        public void GetSuggestionCommentById_ReturnsOkResult_WhenCommentExists()
        {
            var id = Guid.NewGuid();
            var comment = new SuggestionCommentDTO { Id = id, Text = "Test Comment" };
            _mockRepo.Setup(repo => repo.GetById(id)).Returns(comment);

            var result = _controller.GetSuggestionCommentById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedComment = Assert.IsType<SuggestionCommentDTO>(okResult.Value);
            Assert.Equal(id, returnedComment.Id);
        }

        [Fact]
        public void GetSuggestionCommentsBySuggestionId_ReturnsOkResult()
        {
            var suggestionId = Guid.NewGuid();
            var comments = new List<SuggestionCommentDTO>
            {
                new SuggestionCommentDTO { Id = Guid.NewGuid(), SuggestionId = suggestionId }
            };
            _mockRepo.Setup(repo => repo.GetBySuggestionId(suggestionId)).Returns(comments);

            var result = _controller.GetSuggestionCommentsBySuggestionId(suggestionId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedComments = Assert.IsAssignableFrom<IEnumerable<SuggestionCommentDTO>>(okResult.Value);
            Assert.Single(returnedComments);
        }
    }

    public class VoteControllerTests
    {
        private readonly Mock<IVoteRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly VoteController _controller;
        private readonly Mock<LoggerServiceClient> _logger;
        public VoteControllerTests()
        {
            _mockRepo = new Mock<IVoteRepository>();
            _mapper = Mock.Of<IMapper>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new VoteController(_mockRepo.Object, _mapper, _logger.Object);
        }

        [Fact]
        public void GetVotesBySuggestionId_ReturnsOkResult()
        {
            var suggestionId = Guid.NewGuid();
            var votes = new List<VoteDTO>
            {
                new VoteDTO { Id = Guid.NewGuid(), SuggestionId = suggestionId },
                new VoteDTO { Id = Guid.NewGuid(), SuggestionId = suggestionId }
            };
            _mockRepo.Setup(repo => repo.GetBySuggestionId(suggestionId)).Returns(votes);

            var result = _controller.GetVotesBySuggestionId(suggestionId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedVotes = Assert.IsAssignableFrom<IEnumerable<VoteDTO>>(okResult.Value);
            Assert.Equal(2, returnedVotes.Count());
        }
    }

    public class SuggestionCategoryControllerTests
    {
        private readonly Mock<ISuggestionCategoryRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly SuggestionCategoryController _controller;
        private readonly Mock<LoggerServiceClient> _logger;

        public SuggestionCategoryControllerTests()
        {
            _mockRepo = new Mock<ISuggestionCategoryRepository>();
            _mapper = Mock.Of<IMapper>();
            _logger = new Mock<LoggerServiceClient>();
            _controller = new SuggestionCategoryController(_mockRepo.Object, _mapper, _logger.Object);
        }

        [Fact]
        public void GetAllSuggestionCategories_ReturnsOkResult()
        {
            var categories = new List<SuggestionCategoryDTO>
            {
                new SuggestionCategoryDTO { Id = Guid.NewGuid(), Title = "Category 1" },
                new SuggestionCategoryDTO { Id = Guid.NewGuid(), Title = "Category 2" }
            };
            _mockRepo.Setup(repo => repo.GetAll()).Returns(categories);

            var result = _controller.GetAllSuggestionCategories();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCategories = Assert.IsAssignableFrom<IEnumerable<SuggestionCategoryDTO>>(okResult.Value);
            Assert.Equal(2, returnedCategories.Count());
        }

        [Fact]
        public void GetSuggestionCategoryById_ReturnsOkResult_WhenExists()
        {
            var id = Guid.NewGuid();
            var category = new SuggestionCategoryDTO { Id = id, Title = "Category 1" };
            _mockRepo.Setup(repo => repo.GetById(id)).Returns(category);

            var result = _controller.GetSuggestionCategoryById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCategory = Assert.IsType<SuggestionCategoryDTO>(okResult.Value);
            Assert.Equal(id, returnedCategory.Id);
        }
    }
}