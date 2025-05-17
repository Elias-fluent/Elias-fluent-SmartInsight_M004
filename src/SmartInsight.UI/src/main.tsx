import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './styles/index.css'
import App from './App.tsx'
import { Provider } from 'react-redux'
import { legacy_createStore as createStore, applyMiddleware } from 'redux'
import { rootReducer, initialState } from './store/configureStore'
import { apiMiddleware } from './store/middleware/apiMiddleware'

// Create a proper Redux store using the legacy approach to avoid type issues
const store = createStore(
  rootReducer, 
  initialState as any, 
  applyMiddleware(apiMiddleware as any)
)

// Expose store to window object for debug and mockAuth
declare global {
  interface Window {
    store: typeof store;
  }
}
window.store = store;

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Provider store={store}>
      <App />
    </Provider>
  </StrictMode>,
)
