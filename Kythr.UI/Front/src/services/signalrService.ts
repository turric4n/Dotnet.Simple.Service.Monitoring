import * as signalR from '@microsoft/signalr';

let connection: signalR.HubConnection | null = null;
let startPromise: Promise<void> | null = null;

export function getConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('/monitoringhub')
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connection.onclose(() => {
      startPromise = null;
    });
  }
  return connection;
}

export function startConnection(): Promise<void> {
  const conn = getConnection();
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    startPromise = conn.start().catch((err) => {
      startPromise = null;
      throw err;
    });
  }
  return startPromise ?? Promise.resolve();
}
