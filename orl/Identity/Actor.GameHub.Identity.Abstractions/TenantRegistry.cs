using System;

namespace Actor.GameHub.Identity.Abstractions
{
  public static class TenantRegistryConstants
  {
    public const string StorageProviderName = "Identity.TenantRegistry.Storage";
    public const string QueueProviderName = "Identity.TenantRegistry.Queue";
    public const string StreamNamespace = "Identity.TenantRegistry.Stream";
    public static readonly Guid GrainKey = Guid.Empty;
  }

  public abstract class BaseTenantRegistryCommand
  {
    public Guid RequestId { get; init; }
  }

  public class CreateTenantCommand : BaseTenantRegistryCommand
  {
    public string TenantShortname { get; init; }
    public string AdminUsername { get; init; }
    public string AdminPassword { get; init; }
  }

  public abstract class BaseTenantRegistryEvent
  {
    public Guid RequestId { get; init; }
  }

  public class TenantCreatedEvent : BaseTenantRegistryEvent
  {
    public Guid TenantId { get; init; }
    public string TenantShortName { get; init; }
  }

  public class CreateTenantErrorEvent : BaseTenantRegistryEvent
  {
    public int StatusCode { get; init; }
    public string ErrorMessage { get; init; }
  }
}
