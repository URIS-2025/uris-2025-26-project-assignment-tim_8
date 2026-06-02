using AnonymousAPI.Controllers;
using AnonymousRepository.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SuggestionService.Clients;
using SuggestionService.Data;
using SuggestionService.Models.DTOs;
using Xunit;

namespace SuggestionServiceTests
{
    // Task 003: SuggestionService notification producers (suggestion, comment, vote).
    // Verifies each create fires exactly one system notification with the resolved org-id,
    // skips silently when the box is unresolvable, and never breaks the create when the
    // notification call throws.
    public class NotificationProducerTests
    {
        private static ControllerContext WithHttpContext() =>
            new ControllerContext { HttpContext = new DefaultHttpContext() };

        // ---------- SuggestionController ----------

        [Fact]
        public async Task CreateSuggestion_SendsNotification_WithResolvedOrganizationId()
        {
            var boxId = Guid.NewGuid();
            var orgId = Guid.NewGuid();
            var created = new SuggestionCreatedDTO { Id = Guid.NewGuid(), Title = "New", SuggestionBoxId = boxId };

            var repo = new Mock<ISuggestionRepository>();
            repo.Setup(r => r.Create(It.IsAny<SuggestionCreationDTO>())).Returns(created);
            var box = new Mock<SuggestionBoxServiceClient>();
            box.Setup(b => b.TryGetOrganizationIdAsync(boxId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(orgId);
            var notify = new Mock<SystemNotificationServiceClient>();

            var controller = new SuggestionController(repo.Object, new Mock<IMapper>().Object,
                new Mock<LoggerServiceClient>().Object, box.Object, notify.Object)
            { ControllerContext = WithHttpContext() };

            var result = await controller.CreateSuggestion(new SuggestionCreationDTO());

            Assert.IsType<CreatedResult>(result.Result);
            notify.Verify(n => n.TryNotifyAsync(
                It.Is<SystemNotificationCreationDTO>(d => d.OrganizationId == orgId && !string.IsNullOrWhiteSpace(d.Text)),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateSuggestion_BoxMissing_SkipsNotification_StillCreated()
        {
            var created = new SuggestionCreatedDTO { Id = Guid.NewGuid(), SuggestionBoxId = Guid.NewGuid() };

            var repo = new Mock<ISuggestionRepository>();
            repo.Setup(r => r.Create(It.IsAny<SuggestionCreationDTO>())).Returns(created);
            var box = new Mock<SuggestionBoxServiceClient>();
            box.Setup(b => b.TryGetOrganizationIdAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Guid?)null);
            var notify = new Mock<SystemNotificationServiceClient>();

            var controller = new SuggestionController(repo.Object, new Mock<IMapper>().Object,
                new Mock<LoggerServiceClient>().Object, box.Object, notify.Object)
            { ControllerContext = WithHttpContext() };

            var result = await controller.CreateSuggestion(new SuggestionCreationDTO());

            Assert.IsType<CreatedResult>(result.Result);
            notify.Verify(n => n.TryNotifyAsync(
                It.IsAny<SystemNotificationCreationDTO>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateSuggestion_NotificationThrows_StillCreated()
        {
            var orgId = Guid.NewGuid();
            var created = new SuggestionCreatedDTO { Id = Guid.NewGuid(), SuggestionBoxId = Guid.NewGuid() };

            var repo = new Mock<ISuggestionRepository>();
            repo.Setup(r => r.Create(It.IsAny<SuggestionCreationDTO>())).Returns(created);
            var box = new Mock<SuggestionBoxServiceClient>();
            box.Setup(b => b.TryGetOrganizationIdAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(orgId);
            var notify = new Mock<SystemNotificationServiceClient>();
            notify.Setup(n => n.TryNotifyAsync(
                It.IsAny<SystemNotificationCreationDTO>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("notification service down"));

            var controller = new SuggestionController(repo.Object, new Mock<IMapper>().Object,
                new Mock<LoggerServiceClient>().Object, box.Object, notify.Object)
            { ControllerContext = WithHttpContext() };

            var result = await controller.CreateSuggestion(new SuggestionCreationDTO());

            Assert.IsType<CreatedResult>(result.Result);
        }

        // ---------- SuggestionCommentController ----------

        [Fact]
        public async Task CreateSuggestionComment_SendsNotification_WithResolvedOrgAndCommentId()
        {
            var suggestionId = Guid.NewGuid();
            var boxId = Guid.NewGuid();
            var orgId = Guid.NewGuid();
            var created = new SuggestionCommentCreationDTO { Id = Guid.NewGuid(), Text = "Reply", SuggestionId = suggestionId };

            var commentRepo = new Mock<ISuggestionCommentRepository>();
            commentRepo.Setup(r => r.Create(It.IsAny<SuggestionCommentCreationDTO>())).Returns(created);
            var suggestionRepo = new Mock<ISuggestionRepository>();
            suggestionRepo.Setup(r => r.GetById(suggestionId))
                          .Returns(new SuggestionDTO { Id = suggestionId, SuggestionBoxId = boxId });
            var box = new Mock<SuggestionBoxServiceClient>();
            box.Setup(b => b.TryGetOrganizationIdAsync(boxId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(orgId);
            var notify = new Mock<SystemNotificationServiceClient>();

            var controller = new SuggestionCommentController(commentRepo.Object, suggestionRepo.Object,
                new Mock<IMapper>().Object, new Mock<LoggerServiceClient>().Object, box.Object, notify.Object)
            { ControllerContext = WithHttpContext() };

            var result = await controller.CreateSuggestionComment(new SuggestionCommentCreationDTO { SuggestionId = suggestionId });

            Assert.IsType<CreatedResult>(result.Result);
            notify.Verify(n => n.TryNotifyAsync(
                It.Is<SystemNotificationCreationDTO>(d =>
                    d.OrganizationId == orgId &&
                    d.SuggestionCommentId == created.Id &&
                    !string.IsNullOrWhiteSpace(d.Text)),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateSuggestionComment_BoxMissing_SkipsNotification_StillCreated()
        {
            var suggestionId = Guid.NewGuid();
            var created = new SuggestionCommentCreationDTO { Id = Guid.NewGuid(), SuggestionId = suggestionId };

            var commentRepo = new Mock<ISuggestionCommentRepository>();
            commentRepo.Setup(r => r.Create(It.IsAny<SuggestionCommentCreationDTO>())).Returns(created);
            var suggestionRepo = new Mock<ISuggestionRepository>();
            suggestionRepo.Setup(r => r.GetById(suggestionId))
                          .Returns(new SuggestionDTO { Id = suggestionId, SuggestionBoxId = Guid.NewGuid() });
            var box = new Mock<SuggestionBoxServiceClient>();
            box.Setup(b => b.TryGetOrganizationIdAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Guid?)null);
            var notify = new Mock<SystemNotificationServiceClient>();

            var controller = new SuggestionCommentController(commentRepo.Object, suggestionRepo.Object,
                new Mock<IMapper>().Object, new Mock<LoggerServiceClient>().Object, box.Object, notify.Object)
            { ControllerContext = WithHttpContext() };

            var result = await controller.CreateSuggestionComment(new SuggestionCommentCreationDTO { SuggestionId = suggestionId });

            Assert.IsType<CreatedResult>(result.Result);
            notify.Verify(n => n.TryNotifyAsync(
                It.IsAny<SystemNotificationCreationDTO>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ---------- VoteController ----------

        [Fact]
        public async Task CreateVote_SendsNotification_WithResolvedOrganizationId()
        {
            var suggestionId = Guid.NewGuid();
            var boxId = Guid.NewGuid();
            var orgId = Guid.NewGuid();
            var created = new VoteCreationDTO { VoteAuthorId = Guid.NewGuid(), SuggestionId = suggestionId };

            var voteRepo = new Mock<IVoteRepository>();
            voteRepo.Setup(r => r.Create(It.IsAny<VoteCreationDTO>())).Returns(created);
            var suggestionRepo = new Mock<ISuggestionRepository>();
            suggestionRepo.Setup(r => r.GetById(suggestionId))
                          .Returns(new SuggestionDTO { Id = suggestionId, SuggestionBoxId = boxId });
            var box = new Mock<SuggestionBoxServiceClient>();
            box.Setup(b => b.TryGetOrganizationIdAsync(boxId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(orgId);
            var notify = new Mock<SystemNotificationServiceClient>();

            var controller = new VoteController(voteRepo.Object, suggestionRepo.Object, new Mock<IMapper>().Object,
                new Mock<LoggerServiceClient>().Object, box.Object, notify.Object)
            { ControllerContext = WithHttpContext() };

            var result = await controller.CreateVote(new VoteCreationDTO { SuggestionId = suggestionId });

            Assert.IsType<CreatedResult>(result.Result);
            notify.Verify(n => n.TryNotifyAsync(
                It.Is<SystemNotificationCreationDTO>(d => d.OrganizationId == orgId && !string.IsNullOrWhiteSpace(d.Text)),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateVote_BoxMissing_SkipsNotification_StillCreated()
        {
            var suggestionId = Guid.NewGuid();
            var created = new VoteCreationDTO { VoteAuthorId = Guid.NewGuid(), SuggestionId = suggestionId };

            var voteRepo = new Mock<IVoteRepository>();
            voteRepo.Setup(r => r.Create(It.IsAny<VoteCreationDTO>())).Returns(created);
            var suggestionRepo = new Mock<ISuggestionRepository>();
            suggestionRepo.Setup(r => r.GetById(suggestionId))
                          .Returns(new SuggestionDTO { Id = suggestionId, SuggestionBoxId = Guid.NewGuid() });
            var box = new Mock<SuggestionBoxServiceClient>();
            box.Setup(b => b.TryGetOrganizationIdAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Guid?)null);
            var notify = new Mock<SystemNotificationServiceClient>();

            var controller = new VoteController(voteRepo.Object, suggestionRepo.Object, new Mock<IMapper>().Object,
                new Mock<LoggerServiceClient>().Object, box.Object, notify.Object)
            { ControllerContext = WithHttpContext() };

            var result = await controller.CreateVote(new VoteCreationDTO { SuggestionId = suggestionId });

            Assert.IsType<CreatedResult>(result.Result);
            notify.Verify(n => n.TryNotifyAsync(
                It.IsAny<SystemNotificationCreationDTO>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateVote_NotificationThrows_StillCreated()
        {
            var suggestionId = Guid.NewGuid();
            var orgId = Guid.NewGuid();
            var created = new VoteCreationDTO { VoteAuthorId = Guid.NewGuid(), SuggestionId = suggestionId };

            var voteRepo = new Mock<IVoteRepository>();
            voteRepo.Setup(r => r.Create(It.IsAny<VoteCreationDTO>())).Returns(created);
            var suggestionRepo = new Mock<ISuggestionRepository>();
            suggestionRepo.Setup(r => r.GetById(suggestionId))
                          .Returns(new SuggestionDTO { Id = suggestionId, SuggestionBoxId = Guid.NewGuid() });
            var box = new Mock<SuggestionBoxServiceClient>();
            box.Setup(b => b.TryGetOrganizationIdAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(orgId);
            var notify = new Mock<SystemNotificationServiceClient>();
            notify.Setup(n => n.TryNotifyAsync(
                It.IsAny<SystemNotificationCreationDTO>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("notification service down"));

            var controller = new VoteController(voteRepo.Object, suggestionRepo.Object, new Mock<IMapper>().Object,
                new Mock<LoggerServiceClient>().Object, box.Object, notify.Object)
            { ControllerContext = WithHttpContext() };

            var result = await controller.CreateVote(new VoteCreationDTO { SuggestionId = suggestionId });

            Assert.IsType<CreatedResult>(result.Result);
        }
    }
}
