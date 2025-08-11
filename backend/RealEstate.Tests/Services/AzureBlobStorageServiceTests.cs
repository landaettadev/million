using NUnit.Framework;
using NSubstitute;
using Microsoft.Extensions.Configuration;
using RealEstate.Infrastructure.Services;
using FluentAssertions;
using System.Text;

namespace RealEstate.Tests.Services
{
    [TestFixture]
    public class AzureBlobStorageServiceTests
    {
        private AzureBlobStorageService _service;
        private IConfiguration _configuration;

        [SetUp]
        public void Setup()
        {
            _configuration = Substitute.For<IConfiguration>();
            
            // Mock configuration values
            _configuration["AzureStorage:ConnectionString"].Returns("DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=testkey;EndpointSuffix=core.windows.net");
            _configuration["AzureStorage:ContainerName"].Returns("test-container");
            
            _service = new AzureBlobStorageService(_configuration);
        }

        [Test]
        public void Constructor_WithValidConfiguration_ShouldCreateInstance()
        {
            // Act & Assert
            _service.Should().NotBeNull();
            _service.Should().BeOfType<AzureBlobStorageService>();
        }

        [Test]
        public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var action = () => new AzureBlobStorageService(null!);
            action.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Constructor_WithMissingConnectionString_ShouldHandleGracefully()
        {
            // Arrange
            var config = Substitute.For<IConfiguration>();
            config["AzureStorage:ConnectionString"].Returns((string?)null);
            config["AzureStorage:ContainerName"].Returns("test-container");

            // Act & Assert
            var action = () => new AzureBlobStorageService(config);
            action.Should().NotThrow();
        }

        [Test]
        public void Constructor_WithMissingContainerName_ShouldHandleGracefully()
        {
            // Arrange
            var config = Substitute.For<IConfiguration>();
            config["AzureStorage:ConnectionString"].Returns("DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=testkey;EndpointSuffix=core.windows.net");
            config["AzureStorage:ContainerName"].Returns((string?)null);

            // Act & Assert
            var action = () => new AzureBlobStorageService(config);
            action.Should().NotThrow();
        }

        [Test]
        public void GetImageUrl_WithValidImagePath_ShouldReturnValidUrl()
        {
            // Arrange
            var imagePath = "test-image.jpg";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/test-image.jpg";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithEmptyImagePath_ShouldReturnEmptyString()
        {
            // Arrange
            var imagePath = "";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public void GetImageUrl_WithNullImagePath_ShouldReturnEmptyString()
        {
            // Arrange
            string? imagePath = null;

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public void GetImageUrl_WithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var imagePath = "test image with spaces & symbols.jpg";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/test image with spaces & symbols.jpg";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithUrlEncodedCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var imagePath = "test%20image%20encoded.jpg";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/test%20image%20encoded.jpg";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithDifferentConfiguration_ShouldUseCorrectValues()
        {
            // Arrange
            var config = Substitute.For<IConfiguration>();
            config["AzureStorage:ConnectionString"].Returns("DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;EndpointSuffix=core.windows.net");
            config["AzureStorage:ContainerName"].Returns("my-container");
            
            var service = new AzureBlobStorageService(config);
            var imagePath = "test.jpg";
            var expectedUrl = "https://myaccount.blob.core.windows.net/my-container/test.jpg";

            // Act
            var result = service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithInvalidConnectionString_ShouldHandleGracefully()
        {
            // Arrange
            var config = Substitute.For<IConfiguration>();
            config["AzureStorage:ConnectionString"].Returns("invalid-connection-string");
            config["AzureStorage:ContainerName"].Returns("test-container");
            
            var service = new AzureBlobStorageService(config);
            var imagePath = "test.jpg";

            // Act & Assert
            var action = () => service.GetImageUrl(imagePath);
            action.Should().NotThrow();
        }

        [Test]
        public void GetImageUrl_WithVeryLongImagePath_ShouldHandleCorrectly()
        {
            // Arrange
            var imagePath = new string('a', 1000) + ".jpg";
            var expectedUrl = $"https://testaccount.blob.core.windows.net/test-container/{imagePath}";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
            result.Length.Should().Be(1000 + 89); // 89 is the base URL length
        }

        [Test]
        public void GetImageUrl_WithUnicodeCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var imagePath = "test-ñáéíóú-中文-日本語.jpg";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/test-ñáéíóú-中文-日本語.jpg";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithFileExtensions_ShouldPreserveExtensions()
        {
            // Arrange
            var testCases = new[]
            {
                "image.jpg",
                "image.png",
                "image.gif",
                "image.webp",
                "image.bmp",
                "image.tiff"
            };

            foreach (var imagePath in testCases)
            {
                var expectedUrl = $"https://testaccount.blob.core.windows.net/test-container/{imagePath}";

                // Act
                var result = _service.GetImageUrl(imagePath);

                // Assert
                result.Should().Be(expectedUrl);
                result.Should().EndWith(imagePath);
            }
        }

        [Test]
        public void GetImageUrl_WithNestedPaths_ShouldHandleCorrectly()
        {
            // Arrange
            var imagePath = "folder/subfolder/image.jpg";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/folder/subfolder/image.jpg";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithQueryParameters_ShouldHandleCorrectly()
        {
            // Arrange
            var imagePath = "image.jpg?v=123&t=456";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/image.jpg?v=123&t=456";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithHashParameters_ShouldHandleCorrectly()
        {
            // Arrange
            var imagePath = "image.jpg#section1";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/image.jpg#section1";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithMultipleDots_ShouldHandleCorrectly()
        {
            // Arrange
            var imagePath = "my.image.file.jpg";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/my.image.file.jpg";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithNumbers_ShouldHandleCorrectly()
        {
            // Arrange
            var imagePath = "image123.jpg";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/image123.jpg";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithUnderscores_ShouldHandleCorrectly()
        {
            // Arrange
            var imagePath = "my_image_file.jpg";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/my_image_file.jpg";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithHyphens_ShouldHandleCorrectly()
        {
            // Arrange
            var imagePath = "my-image-file.jpg";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/my-image-file.jpg";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithMixedCase_ShouldPreserveCase()
        {
            // Arrange
            var imagePath = "MyImageFile.JPG";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/MyImageFile.JPG";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithLeadingSlash_ShouldHandleCorrectly()
        {
            // Arrange
            var imagePath = "/image.jpg";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container//image.jpg";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithTrailingSlash_ShouldHandleCorrectly()
        {
            // Arrange
            var imagePath = "image.jpg/";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/image.jpg/";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithComplexPath_ShouldHandleCorrectly()
        {
            // Arrange
            var imagePath = "2024/01/15/property-123/image_001.jpg";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/2024/01/15/property-123/image_001.jpg";

            // Act
            var result = _service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithSpecialFileNames_ShouldHandleCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                "file (1).jpg",
                "file[2].png",
                "file{3}.gif",
                "file@4.webp",
                "file#5.bmp",
                "file$6.tiff"
            };

            foreach (var imagePath in testCases)
            {
                var expectedUrl = $"https://testaccount.blob.core.windows.net/test-container/{imagePath}";

                // Act
                var result = _service.GetImageUrl(imagePath);

                // Assert
                result.Should().Be(expectedUrl);
            }
        }

        [Test]
        public void GetImageUrl_WithEnvironmentSpecificConfiguration_ShouldUseCorrectValues()
        {
            // Arrange
            var config = Substitute.For<IConfiguration>();
            config["AzureStorage:ConnectionString"].Returns("DefaultEndpointsProtocol=https;AccountName=devaccount;AccountKey=devkey;EndpointSuffix=core.windows.net");
            config["AzureStorage:ContainerName"].Returns("dev-images");
            
            var service = new AzureBlobStorageService(config);
            var imagePath = "test.jpg";
            var expectedUrl = "https://devaccount.blob.core.windows.net/dev-images/test.jpg";

            // Act
            var result = service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithProductionConfiguration_ShouldUseCorrectValues()
        {
            // Arrange
            var config = Substitute.For<IConfiguration>();
            config["AzureStorage:ConnectionString"].Returns("DefaultEndpointsProtocol=https;AccountName=prodaccount;AccountKey=prodkey;EndpointSuffix=core.windows.net");
            config["AzureStorage:ContainerName"].Returns("prod-images");
            
            var service = new AzureBlobStorageService(config);
            var imagePath = "test.jpg";
            var expectedUrl = "https://prodaccount.blob.core.windows.net/prod-images/test.jpg";

            // Act
            var result = service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithStagingConfiguration_ShouldUseCorrectValues()
        {
            // Arrange
            var config = Substitute.For<IConfiguration>();
            config["AzureStorage:ConnectionString"].Returns("DefaultEndpointsProtocol=https;AccountName=stagingaccount;AccountKey=stagingkey;EndpointSuffix=core.windows.net");
            config["AzureStorage:ContainerName"].Returns("staging-images");
            
            var service = new AzureBlobStorageService(config);
            var imagePath = "test.jpg";
            var expectedUrl = "https://stagingaccount.blob.core.windows.net/staging-images/test.jpg";

            // Act
            var result = service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithCustomEndpointSuffix_ShouldHandleCorrectly()
        {
            // Arrange
            var config = Substitute.For<IConfiguration>();
            config["AzureStorage:ConnectionString"].Returns("DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=testkey;EndpointSuffix=core.cloudapi.de");
            config["AzureStorage:ContainerName"].Returns("test-container");
            
            var service = new AzureBlobStorageService(config);
            var imagePath = "test.jpg";
            var expectedUrl = "https://testaccount.blob.core.cloudapi.de/test-container/test.jpg";

            // Act
            var result = service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithHttpProtocol_ShouldHandleCorrectly()
        {
            // Arrange
            var config = Substitute.For<IConfiguration>();
            config["AzureStorage:ConnectionString"].Returns("DefaultEndpointsProtocol=http;AccountName=testaccount;AccountKey=testkey;EndpointSuffix=core.windows.net");
            config["AzureStorage:ContainerName"].Returns("test-container");
            
            var service = new AzureBlobStorageService(config);
            var imagePath = "test.jpg";
            var expectedUrl = "http://testaccount.blob.core.windows.net/test-container/test.jpg";

            // Act
            var result = service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithHttpsProtocol_ShouldHandleCorrectly()
        {
            // Arrange
            var config = Substitute.For<IConfiguration>();
            config["AzureStorage:ConnectionString"].Returns("DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=testkey;EndpointSuffix=core.windows.net");
            config["AzureStorage:ContainerName"].Returns("test-container");
            
            var service = new AzureBlobStorageService(config);
            var imagePath = "test.jpg";
            var expectedUrl = "https://testaccount.blob.core.windows.net/test-container/test.jpg";

            // Act
            var result = service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithEmptyContainerName_ShouldHandleGracefully()
        {
            // Arrange
            var config = Substitute.For<IConfiguration>();
            config["AzureStorage:ConnectionString"].Returns("DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=testkey;EndpointSuffix=core.windows.net");
            config["AzureStorage:ContainerName"].Returns("");
            
            var service = new AzureBlobStorageService(config);
            var imagePath = "test.jpg";
            var expectedUrl = "https://testaccount.blob.core.windows.net//test.jpg";

            // Act
            var result = service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }

        [Test]
        public void GetImageUrl_WithWhitespaceContainerName_ShouldHandleGracefully()
        {
            // Arrange
            var config = Substitute.For<IConfiguration>();
            config["AzureStorage:ConnectionString"].Returns("DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=testkey;EndpointSuffix=core.windows.net");
            config["AzureStorage:ContainerName"].Returns("   ");
            
            var service = new AzureBlobStorageService(config);
            var imagePath = "test.jpg";
            var expectedUrl = "https://testaccount.blob.core.windows.net/   /test.jpg";

            // Act
            var result = service.GetImageUrl(imagePath);

            // Assert
            result.Should().Be(expectedUrl);
        }
    }
}
