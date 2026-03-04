using AnonymousDomain.Models.Suggestion;
using AnonymousRepository.Repositories;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using SuggestionService.Models.DTOs;
using Xunit;

namespace SuggestionServiceTest
{
    public class SuggestionCategoryRepositoryTests
    {
        private SuggestionContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<SuggestionContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            return new SuggestionContext(options, configMock.Object);
        }

        private IMapper GetMapper()
        {
            return Mock.Of<IMapper>();
        }

        [Fact]
        public void GetAll_ReturnsAllCategories()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var categories = new List<SuggestionCategory>
            {
                new SuggestionCategory { Id = Guid.NewGuid(), Title = "Category 1", Description = "Desc 1" },
                new SuggestionCategory { Id = Guid.NewGuid(), Title = "Category 2", Description = "Desc 2" }
            };
            context.SuggestionCategories.AddRange(categories);
            context.SaveChanges();

            var categoryDtos = categories.Select(c => new SuggestionCategoryDTO { Id = c.Id, Title = c.Title, Description = c.Description }).ToList();
            mapper.Setup(m => m.Map<IEnumerable<SuggestionCategoryDTO>>(It.IsAny<IEnumerable<SuggestionCategory>>())).Returns(categoryDtos);

            var repo = new SuggestionCategoryRepository(context, mapper.Object);

            var result = repo.GetAll();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetAll_ReturnsEmpty_WhenNoCategories()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<IEnumerable<SuggestionCategoryDTO>>(It.IsAny<IEnumerable<SuggestionCategory>>())).Returns(new List<SuggestionCategoryDTO>());

            var repo = new SuggestionCategoryRepository(context, mapper.Object);

            var result = repo.GetAll();

            Assert.Empty(result);
        }

        [Fact]
        public void GetById_ReturnsCategory_WhenExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var id = Guid.NewGuid();
            var category = new SuggestionCategory { Id = id, Title = "Category 1", Description = "Desc 1" };
            context.SuggestionCategories.Add(category);
            context.SaveChanges();

            var categoryDto = new SuggestionCategoryDTO { Id = id, Title = "Category 1", Description = "Desc 1" };
            mapper.Setup(m => m.Map<SuggestionCategoryDTO>(It.IsAny<SuggestionCategory>())).Returns(categoryDto);

            var repo = new SuggestionCategoryRepository(context, mapper.Object);

            var result = repo.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public void GetById_ReturnsNull_WhenNotExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<SuggestionCategoryDTO>(null)).Returns((SuggestionCategoryDTO)null);

            var repo = new SuggestionCategoryRepository(context, mapper.Object);

            var result = repo.GetById(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public void Create_AddsCategory_AndReturnsDTO()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var categoryDto = new SuggestionCategoryDTO { Id = Guid.NewGuid(), Title = "New Category", Description = "Desc" };
            var category = new SuggestionCategory { Id = categoryDto.Id, Title = categoryDto.Title, Description = categoryDto.Description };

            mapper.Setup(m => m.Map<SuggestionCategory>(categoryDto)).Returns(category);
            mapper.Setup(m => m.Map<SuggestionCategoryDTO>(category)).Returns(categoryDto);

            var repo = new SuggestionCategoryRepository(context, mapper.Object);

            var result = repo.Create(categoryDto);

            Assert.NotNull(result);
            Assert.Equal("New Category", result.Title);
            Assert.Equal(1, context.SuggestionCategories.Count());
        }

        [Fact]
        public void Update_UpdatesCategory_AndReturnsDTO()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var id = Guid.NewGuid();
            var category = new SuggestionCategory { Id = id, Title = "Old Title", Description = "Old Desc" };
            context.SuggestionCategories.Add(category);
            context.SaveChanges();

            var updateDto = new SuggestionCategoryDTO { Id = id, Title = "Updated Title", Description = "Updated Desc" };
            var updatedCategory = new SuggestionCategory { Id = id, Title = "Updated Title", Description = "Updated Desc" };

            mapper.Setup(m => m.Map(updateDto, It.IsAny<SuggestionCategory>())).Callback<SuggestionCategoryDTO, SuggestionCategory>((dto, cat) =>
            {
                cat.Title = dto.Title;
                cat.Description = dto.Description;
            });
            mapper.Setup(m => m.Map<SuggestionCategoryDTO>(It.IsAny<SuggestionCategory>())).Returns(updateDto);

            var repo = new SuggestionCategoryRepository(context, mapper.Object);

            var result = repo.Update(updateDto);

            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
        }

        [Fact]
        public void Delete_RemovesCategory_WhenExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var id = Guid.NewGuid();
            var category = new SuggestionCategory { Id = id, Title = "Category", Description = "Desc" };
            context.SuggestionCategories.Add(category);
            context.SaveChanges();

            var repo = new SuggestionCategoryRepository(context, mapper.Object);

            repo.Delete(id);

            Assert.Equal(0, context.SuggestionCategories.Count());
        }

        [Fact]
        public void Delete_DoesNotThrow_WhenCategoryNotExists()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var repo = new SuggestionCategoryRepository(context, mapper.Object);

            var exception = Record.Exception(() => repo.Delete(Guid.NewGuid()));

            Assert.Null(exception);
        }

        [Fact]
        public void SaveChanges_ReturnsTrue_WhenChangesSaved()
        {
            var context = GetInMemoryContext();
            var mapper = new Mock<IMapper>();

            var category = new SuggestionCategory { Id = Guid.NewGuid(), Title = "Category", Description = "Desc" };
            context.SuggestionCategories.Add(category);

            var repo = new SuggestionCategoryRepository(context, mapper.Object);

            var result = repo.SaveChanges();

            Assert.True(result);
        }
    }
}