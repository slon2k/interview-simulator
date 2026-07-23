import { createBrowserRouter } from 'react-router-dom'
import { RootLayout } from '../components/layout/RootLayout'
import { ProtectedRoute } from '../components/routing/ProtectedRoute'
import { LandingPage } from '../features/landing/LandingPage'
import { DashboardPage } from '../features/dashboard/DashboardPage'
import { InterviewSetupPage } from '../features/interview/InterviewSetupPage'
import { SessionHistoryPage } from '../features/sessions/SessionHistoryPage'
import { SessionDetailPage } from '../features/sessions/SessionDetailPage'
import { UnauthorizedPage } from '../pages/UnauthorizedPage'
import { NotFoundPage } from '../pages/NotFoundPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      {
        index: true,
        element: <LandingPage />,
      },
      {
        path: 'dashboard',
        element: (
          <ProtectedRoute>
            <DashboardPage />
          </ProtectedRoute>
        ),
      },
      {
        path: 'interview/new',
        element: (
          <ProtectedRoute>
            <InterviewSetupPage />
          </ProtectedRoute>
        ),
      },
      {
        path: 'history',
        element: (
          <ProtectedRoute>
            <SessionHistoryPage />
          </ProtectedRoute>
        ),
      },
      {
        path: 'history/:sessionId',
        element: (
          <ProtectedRoute>
            <SessionDetailPage />
          </ProtectedRoute>
        ),
      },
      {
        path: 'unauthorized',
        element: <UnauthorizedPage />,
      },
      {
        path: '*',
        element: <NotFoundPage />,
      },
    ],
  },
])
