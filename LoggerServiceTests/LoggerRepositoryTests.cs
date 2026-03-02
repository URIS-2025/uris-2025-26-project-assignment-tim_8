using LoggerService.Context;
using LoggerService.Data;
using LoggerService.Models;
using LoggerService.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoggerServiceTests
{
    public class LoggerRepositoryTests
    {
        private LoggerContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<LoggerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Pravimo konfiguraciju sa dummy connection stringom
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
            { "ConnectionStrings:DefaultConnection", "dummy" }
                })
                .Build();

            return new LoggerContext(options, config);
        }

        // ============ GET ALL ============

        [Fact]
        public void GetAll_WhenLogsExist_ReturnsCorrectCount()
        {
            // Arrange
            var context = CreateInMemoryContext();
            context.Logs.AddRange(
                new Log { Id = Guid.NewGuid(), Action = "Action1", ServiceName = "LoggerService", Timestamp = DateTime.UtcNow },
                new Log { Id = Guid.NewGuid(), Action = "Action2", ServiceName = "LoggerService", Timestamp = DateTime.UtcNow }
            );
            context.SaveChanges();
            var repo = new LoggerRepository(context);

            // Act
            var result = repo.GetAll(100);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetAll_WhenNoLogs_ReturnsEmpty()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var repo = new LoggerRepository(context);

            // Act
            var result = repo.GetAll(100);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetAll_RespectsTheTakeParameter()
        {
            // Arrange
            var context = CreateInMemoryContext();
            context.Logs.AddRange(
                new Log { Id = Guid.NewGuid(), Action = "Action1", Timestamp = DateTime.UtcNow },
                new Log { Id = Guid.NewGuid(), Action = "Action2", Timestamp = DateTime.UtcNow },
                new Log { Id = Guid.NewGuid(), Action = "Action3", Timestamp = DateTime.UtcNow }
            );
            context.SaveChanges();
            var repo = new LoggerRepository(context);

            // Act
            var result = repo.GetAll(take: 2);

            // Assert
            Assert.Equal(2, result.Count());
        }

        // ============ GET BY ID ============

        [Fact]
        public void GetById_WhenLogExists_ReturnsCorrectLog()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var id = Guid.NewGuid();
            context.Logs.Add(new Log { Id = id, Action = "TestAction", Timestamp = DateTime.UtcNow });
            context.SaveChanges();
            var repo = new LoggerRepository(context);

            // Act
            var result = repo.GetById(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public void GetById_WhenLogDoesNotExist_ReturnsNull()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var repo = new LoggerRepository(context);

            // Act
            var result = repo.GetById(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        // ============ CREATE ============

        [Fact]
        public void Create_WhenValid_SavesLogToDatabase()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var repo = new LoggerRepository(context);
            var dto = new LogCreationDTO
            {
                Action = "Create",
                ServiceName = "LoggerService",
                IsSuccess = true
            };

            // Act
            var result = repo.Create(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, context.Logs.Count());
            Assert.Equal("Create", result.Action);
        }

        [Fact]
        public void Create_WhenValid_ReturnsCorrectDTO()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var repo = new LoggerRepository(context);
            var dto = new LogCreationDTO
            {
                Action = "Create",
                ServiceName = "LoggerService",
                UserId = "user-123",
                HttpMethod = "POST",
                IsSuccess = true
            };

            // Act
            var result = repo.Create(dto);

            // Assert
            Assert.Equal(dto.Action, result.Action);
            Assert.Equal(dto.ServiceName, result.ServiceName);
            Assert.Equal(dto.UserId, result.UserId);
            Assert.Equal(dto.HttpMethod, result.HttpMethod);
        }

        // ============ DELETE ============

        [Fact]
        public void Delete_WhenLogExists_RemovesFromDatabase()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var id = Guid.NewGuid();
            context.Logs.Add(new Log { Id = id, Action = "TestAction", Timestamp = DateTime.UtcNow });
            context.SaveChanges();
            var repo = new LoggerRepository(context);

            // Act
            repo.Delete(id);

            // Assert
            Assert.Equal(0, context.Logs.Count());
        }

        [Fact]
        public void Delete_WhenLogDoesNotExist_DoesNotThrow()
        {
            // Arrange
            var context = CreateInMemoryContext();
            var repo = new LoggerRepository(context);

            // Act & Assert
            var exception = Record.Exception(() => repo.Delete(Guid.NewGuid()));
            Assert.Null(exception); // ne baca exception
        }

        // ============ SEARCH ============

        [Fact]
        public void Search_ByAction_ReturnsCorrectLogs()
        {
            // Arrange
            var context = CreateInMemoryContext();
            context.Logs.AddRange(
                new Log { Id = Guid.NewGuid(), Action = "Create", Timestamp = DateTime.UtcNow },
                new Log { Id = Guid.NewGuid(), Action = "Delete", Timestamp = DateTime.UtcNow }
            );
            context.SaveChanges();
            var repo = new LoggerRepository(context);

            // Act
            var result = repo.Search(null, "Create", null, null, null, null, null, null, 100);

            // Assert
            Assert.Single(result);
            Assert.Equal("Create", result.First().Action);
        }

        [Fact]
        public void Search_ByIsSuccess_ReturnsCorrectLogs()
        {
            // Arrange
            var context = CreateInMemoryContext();
            context.Logs.AddRange(
                new Log { Id = Guid.NewGuid(), Action = "Action1", IsSuccess = true, Timestamp = DateTime.UtcNow },
                new Log { Id = Guid.NewGuid(), Action = "Action2", IsSuccess = false, Timestamp = DateTime.UtcNow }
            );
            context.SaveChanges();
            var repo = new LoggerRepository(context);

            // Act
            var result = repo.Search(null, null, null, null, null, false, null, null, 100);

            // Assert
            Assert.Single(result);
            Assert.False(result.First().IsSuccess);
        }

        [Fact]
        public void Search_ByDateRange_ReturnsCorrectLogs()
        {
            // Arrange
            var context = CreateInMemoryContext();
            context.Logs.AddRange(
                new Log { Id = Guid.NewGuid(), Action = "Old", Timestamp = DateTime.UtcNow.AddDays(-10) },
                new Log { Id = Guid.NewGuid(), Action = "New", Timestamp = DateTime.UtcNow }
            );
            context.SaveChanges();
            var repo = new LoggerRepository(context);

            // Act
            var result = repo.Search(null, null, null, null, null, null, DateTime.UtcNow.AddDays(-1), null, 100);

            // Assert
            Assert.Single(result);
            Assert.Equal("New", result.First().Action);
        }

        [Fact]
        public void Search_WhenNoMatch_ReturnsEmpty()
        {
            // Arrange
            var context = CreateInMemoryContext();
            context.Logs.Add(new Log { Id = Guid.NewGuid(), Action = "Create", Timestamp = DateTime.UtcNow });
            context.SaveChanges();
            var repo = new LoggerRepository(context);

            // Act
            var result = repo.Search(null, "NonExistentAction", null, null, null, null, null, null, 100);

            // Assert
            Assert.Empty(result);
        }
    }
}

