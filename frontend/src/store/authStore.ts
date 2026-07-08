import { create } from 'zustand'

// What we keep about a logged-in user (only SAFE info — never the JWT token).
// The token stays in the HttpOnly cookie, which JavaScript can't read.
type User = {
  id: number // NEW — needed for chat
  fullName: string
  role: string   // "Employee" or "Manager"
}

// The shape of our store: the data + the actions that change it.
type AuthState = {
  user: User | null        // null means nobody is logged in
  isLoggedIn: boolean
  login: (user: User) => void   // call this after a successful login
  logout: () => void            // call this on logout
}

// create() makes a store. `set` is how we update the values inside it.
export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isLoggedIn: false,
  login: (user) => set({ user: user, isLoggedIn: true }),
  logout: () => set({ user: null, isLoggedIn: false }),
}))