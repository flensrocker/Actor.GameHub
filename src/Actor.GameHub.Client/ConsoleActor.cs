using Actor.GameHub.Terminal.Abstractions;
using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Event;

namespace Actor.GameHub.Client
{
  public class ConsoleActor : ReceiveActor
  {
    private readonly ILoggingAdapter _logger = Context.GetLogger();

    private IActorRef? _consoleRef;
    private IActorRef _clusterClient = null!;
    private TerminalOpenSuccessMsg? _terminalSession;

    public ConsoleActor()
    {
      Become(ReceiveOpen);

      _logger.Info("==> Console started");
    }

    private void ReceiveOpen()
    {
      Receive<OpenTerminalMsg>(Open);
      Receive<TerminalOpenErrorMsg>(OpenError);
      Receive<TerminalOpenSuccessMsg>(OpenSuccess);
    }

    private void ReceiveInput()
    {
      Receive<InputTerminalMsg>(Input);
      Receive<TerminalInputErrorMsg>(InputError);
      Receive<TerminalInputSuccessMsg>(InputSuccess);
      Receive<CloseTerminalMsg>(Close);
    }

    protected override void PreStart()
    {
      _clusterClient = Context.System.ActorOf(ClusterClient.Props(ClusterClientSettings.Create(Context.System)));
    }

    private void Open(OpenTerminalMsg openMsg)
    {
      _logger.Info($"open terminal from {Sender.Path}");

      _consoleRef = Sender;
      _clusterClient.Tell(new ClusterClient.Send(TerminalMetadata.TerminalPath, openMsg));
    }

    private void OpenError(TerminalOpenErrorMsg terminalErrorMsg)
    {
      System.Diagnostics.Debug.Assert(_consoleRef is not null);

      _consoleRef.Tell(terminalErrorMsg);
    }

    private void OpenSuccess(TerminalOpenSuccessMsg terminalSession)
    {
      System.Diagnostics.Debug.Assert(_terminalSession is null && _consoleRef is not null);

      _terminalSession = terminalSession;
      _consoleRef.Tell(terminalSession);

      Become(ReceiveInput);
    }

    private void Input(InputTerminalMsg inputTerminalMsg)
    {
      System.Diagnostics.Debug.Assert(_terminalSession is not null);

      _clusterClient.Tell(new ClusterClient.Send(_terminalSession.TerminalRef.Path.ToStringWithoutAddress(), inputTerminalMsg));
    }

    private void InputError(TerminalInputErrorMsg inputErrorMsg)
    {
      System.Diagnostics.Debug.Assert(_consoleRef is not null);

      _consoleRef.Tell(inputErrorMsg);
    }

    private void InputSuccess(TerminalInputSuccessMsg inputSuccessMsg)
    {
      System.Diagnostics.Debug.Assert(_consoleRef is not null);

      _consoleRef.Tell(inputSuccessMsg);
    }

    private void Close(CloseTerminalMsg closeMsg)
    {
      System.Diagnostics.Debug.Assert(_terminalSession is not null);

      _clusterClient.Tell(new ClusterClient.Send(_terminalSession.TerminalRef.Path.ToStringWithoutAddress(), closeMsg));

      _terminalSession = null;
      _consoleRef = null;

      Become(ReceiveOpen);
    }

    public static Props Props()
      => Akka.Actor.Props.Create<ConsoleActor>();
  }
}
