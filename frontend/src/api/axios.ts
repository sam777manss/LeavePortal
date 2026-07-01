import axios from 'axios'

// One axios instance that ALL our API calls will use.
// We set it up once here, then import it everywhere.
const api = axios.create({

  // Vite swaps this in from the matching .env file at build/dev time.
  // Dev (npm run dev)  -> .env.development -> local API
  // Build (npm run build) -> .env.production -> deployed Azure API
  baseURL: import.meta.env.VITE_API_URL,

  // THIS is the important line for your JWT cookie:
  // it tells the browser to SEND the HttpOnly cookie with every request.
  withCredentials: true,
})

export default api