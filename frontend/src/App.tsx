import { Routes, Route } from 'react-router-dom'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'

function App() {
  return (
    // Routes = the list of pages. Each Route maps a URL to a component.
    <Routes>
      {/* "/" is the home URL — show the Login page there */}
      <Route path="/" element={<LoginPage />} />
      <Route path='/dashboard' element={<DashboardPage/>} />
    </Routes>
  )
}

export default App