using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Moq;
using Notification.Application.UseCases.SendTicketNotification;
using Notification.Infrastructure.Events;
using System.Text.Json;
using Xunit;
using System.Reflection;

namespace Notification.Infrastructure.UnitTests.Events;

public class TicketIssuedEventConsumerTests
{
    private readonly Mock<IConsumer<string, string>> _mockConsumer;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<TicketIssuedEventConsumer>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServicusing Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hostin susing MediatR;
using ocusing Microsoewusing Microsoft.Extensions.Logging;
using MicrMousing Microsoft.Extensions.Optionserusing Microsoft.Extensions.Hosting =using Moq;
using Notification.App      _mockSerusing Notification.Infrastructure.Events;
using System.Text.Jspeusing System.Text.Json;
using Xunit;
usi()using Xunit;
using Sysnsusing SysteaO
namespace Notification   
public class TicketIssuedEventConsumerTests
{
    priCon{
    private readonly Mock<IConsumer<stri Top    private readonly Mock<IMediator> _mockMediator;
    private ret-    private readonly Mock<ILogger<TicketIssuedEvende    private readonly Mock<IServiceProvider> _mockServiceProvider;
    priur    private readonly Mock<IServiceScope> _mockServiceScope;
    ct    private readonly Mock<IServicusing Confluent.Kafka;
usSeusing MediatR;
using Microsoft.Extensions.DependencyIn=>using Microsoviusing Microsoft.Extensions.Logging;
using Micr.Ousing Microsoft.Extensions.Optionserusing Microsoft.Extensions.Hostin edusing ocusing Microsoewusing Microsoft.Extensionctusing MicrMousing Microsoft.Extensions.Optionserusing Micheusing Notification.App      _mockSerusing Notification.Infrastructure.Events;
using System.Teenusing System.Text.Jspeusing System.Text.Json;
using Xunit;
usi()using Xunit;d.using Xunit;
usi()using Xunit;
using Sysnsust@usi()using "using Sysnsusingennamespace Notification ,public
