import * as signalR from '@microsoft/signalr'

// Our hub lives at "/chathub" (NOT under "/api").
// VITE_API_URL is ".../api" (dev) or "/api" (prod) — strip the "/api" and add "/chathub".
const apiBase = import.meta.env.VITE_API_URL as string
const hubUrl = apiBase.replace(/\/api$/, '') + '/chathub'

// One SignalR connection to the ChatHub.
export const connection = new signalR.HubConnectionBuilder()
  .withUrl(hubUrl, {
    withCredentials: true,   // send the JWT cookie so the hub knows who we are
  })
  .withAutomaticReconnect()  // reconnect automatically if the connection drops
  .build()