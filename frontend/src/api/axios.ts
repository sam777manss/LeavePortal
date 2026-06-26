import axios from 'axios'

// One axios instance that ALL our API calls will use.
// We set it up once here, then import it everywhere.
const api = axios.create({
  // Your .NET API address. All calls add to this,
  // e.g. api.post('/auth/login') -> https://localhost:7147/api/auth/login
  baseURL: 'https://localhost:7147/api',

  // THIS is the important line for your JWT cookie:
  // it tells the browser to SEND the HttpOnly cookie with every request.
  withCredentials: true,
})

export default api