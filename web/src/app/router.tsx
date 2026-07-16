import { createBrowserRouter } from 'react-router-dom'
import { RootLayout } from '../components/layout/RootLayout'
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
        element: <DashboardPage />,
      },
      {
        path: 'interview/new',
        element: <InterviewSetupPage />,
      },
      {
        path: 'history',
        element: <SessionHistoryPage />,
      },
      {
        path: 'history/:sessionId',
        element: <SessionDetailPage />,
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
