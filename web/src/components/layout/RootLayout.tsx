import { AppShell, Container } from '@mantine/core'
import { Outlet } from 'react-router-dom'
import { Header } from './Header'

export function RootLayout() {
  return (
    <AppShell header={{ height: 64 }} padding="md">
      <Header />

      <AppShell.Main>
        <Container size="lg">
          <Outlet />
        </Container>
      </AppShell.Main>
    </AppShell>
  )
}
