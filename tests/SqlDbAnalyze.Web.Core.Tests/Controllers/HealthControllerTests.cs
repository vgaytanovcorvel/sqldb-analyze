using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SqlDbAnalyze.Web.Core.Controllers;
using Xunit;

namespace SqlDbAnalyze.Web.Core.Tests.Controllers;

public class HealthControllerTests
{
    [Fact]
    public void Get_Should_ReturnOk_When_Called()
    {
        // Arrange
        var controller = new HealthController();

        // Act
        var result = controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Get_Should_ReturnHealthyStatus_When_Called()
    {
        // Arrange
        var controller = new HealthController();

        // Act
        var result = controller.Get() as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        var value = result!.Value;
        value.Should().NotBeNull();
        value!.ToString().Should().Contain("Healthy");
    }
}
