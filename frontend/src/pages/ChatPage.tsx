import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { HubConnectionState } from '@microsoft/signalr'
import api from '../api/axios'
import { connection } from '../api/chatConnection'
import { useAuthStore } from '../store/authStore'
import Navbar from '../components/Navbar'

type ChatUser = { id: number; fullName: string; role: string }
type ChatMessage = { id?: number; senderId: number; receiverId: number; content: string; sentAt: string }

function ChatPage() {
  const myId = useAuthStore((s) => s.user?.id)

  const [selectedUser, setSelectedUser] = useState<ChatUser | null>(null)
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [text, setText] = useState('')

  // Load the list of people I can chat with.
  const { data: users } = useQuery({
    queryKey: ['chatUsers'],
    queryFn: async () => {
      const res = await api.get('/chat/users')
      return res.data as ChatUser[]
    },
  })

  // Start the SignalR connection once when the page opens.
  useEffect(() => {
    if (connection.state === HubConnectionState.Disconnected) {
      connection.start().catch((err) => console.error('SignalR connection error:', err))
    }
  }, [])

  // When I pick a user: load history + listen for live messages for THIS conversation.
  useEffect(() => {
    if (!selectedUser) return

    // load history
    api.get(`/chat/history/${selectedUser.id}`).then((res) => {
      setMessages(res.data as ChatMessage[])
    })

    // handler for live incoming messages
    function handleReceive(msg: ChatMessage) {
      // only add if it belongs to the conversation I'm viewing
      if (msg.senderId === selectedUser!.id || msg.receiverId === selectedUser!.id) {
        setMessages((prev) => [...prev, msg])
      }
    }

    connection.on('ReceiveMessage', handleReceive)

    // cleanup when I switch user or leave the page
    return () => {
      connection.off('ReceiveMessage', handleReceive)
    }
  }, [selectedUser])

  // Send a message through the hub.
  async function handleSend() {
    if (!selectedUser || text.trim() === '') return
    await connection.invoke('SendMessage', selectedUser.id, text)
    setText('')
    // no manual add — the hub echoes the message back to us via ReceiveMessage
  }

  return (
    <>
      <Navbar />
      <div className="container mt-4">
        <h4>Chat</h4>
        <div className="row">
          {/* LEFT: contact list */}
          <div className="col-4">
            <div className="list-group">
              {users?.map((u) => (
                <button
                  key={u.id}
                  className={'list-group-item list-group-item-action' + (selectedUser?.id === u.id ? ' active' : '')}
                  onClick={() => setSelectedUser(u)}
                >
                  {u.fullName} <small>({u.role})</small>
                </button>
              ))}
            </div>
          </div>

          {/* RIGHT: conversation */}
          <div className="col-8">
            {!selectedUser ? (
              <p>Select someone to start chatting.</p>
            ) : (
              <div>
                <h5>{selectedUser.fullName}</h5>

                <div className="border p-3 mb-2" style={{ height: '350px', overflowY: 'auto' }}>
                  {messages.map((m, index) => (
                    <div
                      key={m.id ?? index}
                      className={'mb-2 ' + (m.senderId === myId ? 'text-end' : 'text-start')}
                    >
                      <span className="d-inline-block border p-2">{m.content}</span>
                    </div>
                  ))}
                </div>

                <div className="d-flex gap-2">
                  <input
                    className="form-control"
                    placeholder="Type a message..."
                    value={text}
                    onChange={(e) => setText(e.target.value)}
                    onKeyDown={(e) => { if (e.key === 'Enter') handleSend() }}
                  />
                  <button className="btn btn-primary" onClick={handleSend}>Send</button>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </>
  )
}

export default ChatPage