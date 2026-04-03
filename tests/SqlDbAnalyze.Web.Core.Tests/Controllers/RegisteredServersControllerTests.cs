using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Web.Core.Controllers;
using Xunit;

namespace SqlDbAnalyze.Web.Core.Tests.Controllers;

public class RegisteredServersControllerTests
{
    private readonly IRegisteredServerService _service = Substitute.For<IRegisteredServerService>();

    [Fact]
    public async Task GetAll_Should_ReturnOkWithServers_When_ServersExist()
    {
        // Arrange
        var servers = new List<RegisteredServer>
        {
            new(1, "Server1", "sub", "rg", "sql", DateTimeOffset.UtcNow)
        };
        _service.GetAllServersAsync(Arg.Any<CancellationToken>()).Returns(servers);
        var controller = new RegisteredServersController(_service);

        // Act
        var result = await controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IReadOnlyList<RegisteredServer>>().Subject;
        data.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_Should_ReturnCreatedAtAction_When_ValidRequest()
    {
        // Arrange
        var request = new CreateRegisteredServerRequest("Test", "sub", "rg", "sql");
        var created = new RegisteredServer(1, "Test", "sub", "rg", "sql", DateTimeOffset.UtcNow);
        _service.CreateServerAsync(request, Arg.Any<CancellationToken>()).Returns(created);
        var controller = new RegisteredServersController(_service);

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().Be(created);
    }

    [Fact]
    public async Task Delete_Should_ReturnNoContent_When_ServerExists()
    {
        // Arrange
        var controller = new RegisteredServersController(_service);

        // Act
        var result = await controller.Delete(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _service.Received(1).DeleteServerAsync(1, Arg.Any<CancellationToken>());
    }
}
