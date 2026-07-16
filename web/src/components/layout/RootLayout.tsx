import {
  AppShell,
  Button,
  Container,
  Group,
  Title
} from "@mantine/core";
import {
  Link as RouterLink,
  Outlet,
  useLocation
} from "react-router-dom";

const navItems = [
  { label: "Home", to: "/" },
  { label: "Dashboard", to: "/dashboard" },
  { label: "Interview", to: "/interview/new" },
  { label: "History", to: "/history" }
];

export function RootLayout() {
  const location = useLocation();

  function isActive(to: string) {
    if (to === "/") {
      return location.pathname === "/";
    }

    return location.pathname.startsWith(to);
  }

  return (
    <AppShell header={{ height: 64 }} padding="md">
      <AppShell.Header>
        <Container size="lg" h="100%">
          <Group h="100%" justify="space-between" wrap="nowrap">
            <Title order={3} textWrap="nowrap">
              AI Interview Simulator
            </Title>

            <Group gap="xs" wrap="nowrap">
              {navItems.map((item) => (
                <Button
                  key={item.to}
                  component={RouterLink}
                  to={item.to}
                  variant={isActive(item.to) ? "light" : "subtle"}
                  color={isActive(item.to) ? "blue" : "gray"}
                >
                  {item.label}
                </Button>
              ))}

              <Button component="a" href="/login" variant="filled">
                Sign in
              </Button>
            </Group>
          </Group>
        </Container>
      </AppShell.Header>

      <AppShell.Main>
        <Container size="lg">
          <Outlet />
        </Container>
      </AppShell.Main>
    </AppShell>
  );
}