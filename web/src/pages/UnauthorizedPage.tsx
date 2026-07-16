import { Button, Stack, Text, Title } from "@mantine/core";

export function UnauthorizedPage() {
  return (
    <Stack>
      <Title>Access denied</Title>
      <Text c="dimmed">
        You are signed in, but your account is not invited to use this app.
      </Text>
      <Button component="a" href="/logout" variant="light">
        Sign out
      </Button>
    </Stack>
  );
}