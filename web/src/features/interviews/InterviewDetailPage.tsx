import {
  Alert,
  Badge,
  Button,
  Card,
  Group,
  Loader,
  Modal,
  Stack,
  Text,
  Textarea,
  Title,
} from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { useForm } from '@mantine/form'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError } from '../../api/apiError'
import {
  getInterview,
  startInterview,
  submitAnswer,
  completeInterview,
  type GetInterviewResponse,
} from '../../api/interviewApi'
import { toCount } from './interviewHelpers'

type InterviewQuery = ReturnType<typeof useQuery<GetInterviewResponse>>

export function InterviewDetailPage() {
  const { interviewId } = useParams<{ interviewId: string }>()

  const interviewQuery = useQuery({
    queryKey: ['interview', interviewId],
    queryFn: () => getInterview(interviewId!),
    enabled: !!interviewId,
  })

  return (
    <Stack gap="md">
      <Group justify="space-between" align="center">
        <Title>Interview</Title>
        <Button component={RouterLink} to="/interviews" variant="light">
          Back to interviews
        </Button>
      </Group>
      <InterviewBody interviewId={interviewId!} query={interviewQuery} />
    </Stack>
  )
}

function InterviewBody({ interviewId, query }: { interviewId: string; query: InterviewQuery }) {
  if (query.isLoading) {
    return (
      <Group>
        <Loader size="sm" />
        <Text c="dimmed">Loading interview...</Text>
      </Group>
    )
  }

  if (query.isError) {
    const message =
      query.error instanceof ApiError ? query.error.message : 'Unable to load interview.'
    return (
      <>
        <Alert color="red" title="Could not load interview">
          {message}
        </Alert>
        <Group>
          <Button variant="light" onClick={() => void query.refetch()}>
            Retry
          </Button>
        </Group>
      </>
    )
  }

  const interview = query.data
  if (!interview) return null

  const status = interview.status.toLowerCase()

  if (status === 'created') {
    return <CreatedInterview interviewId={interviewId} interview={interview} />
  }

  if (status === 'active') {
    return <ActiveInterview interviewId={interviewId} interview={interview} />
  }

  if (status === 'completed') {
    return <CompletedInterview interview={interview} />
  }

  return <Text c="dimmed">Unknown interview status: {interview.status}</Text>
}

function InterviewMeta({ interview }: { interview: GetInterviewResponse }) {
  return (
    <Card withBorder radius="md">
      <Stack gap="xs">
        <Group gap="xs">
          <Text fw={600}>{interview.targetRole}</Text>
          <Text c="dimmed">·</Text>
          <Text c="dimmed">{interview.focusArea}</Text>
        </Group>
        <Group gap="xs">
          <Badge variant="light">{interview.interviewType}</Badge>
          <Badge variant="light" color="gray">
            {interview.seniorityLevel}
          </Badge>
          <Text size="sm" c="dimmed">
            {toCount(interview.answeredCount)} / {toCount(interview.questionCount)} questions
          </Text>
        </Group>
      </Stack>
    </Card>
  )
}

function CreatedInterview({
  interviewId,
  interview,
}: {
  interviewId: string
  interview: GetInterviewResponse
}) {
  const queryClient = useQueryClient()

  const startMutation = useMutation({
    mutationFn: () => startInterview(interviewId),
    onSuccess: (data) =>
      queryClient.setQueryData<GetInterviewResponse>(['interview', interviewId], {
        ...(data as unknown as GetInterviewResponse),
        feedback: null,
      }),
  })

  const errorMessage =
    startMutation.error instanceof ApiError
      ? startMutation.error.message
      : startMutation.isError
        ? 'Could not start interview.'
        : null

  return (
    <Stack gap="md">
      <InterviewMeta interview={interview} />
      {errorMessage && (
        <Alert color="red" title="Could not start interview">
          {errorMessage}
        </Alert>
      )}
      <Group>
        <Button loading={startMutation.isPending} onClick={() => startMutation.mutate()}>
          Start interview
        </Button>
      </Group>
    </Stack>
  )
}

function ActiveInterview({
  interviewId,
  interview,
}: {
  interviewId: string
  interview: GetInterviewResponse
}) {
  const queryClient = useQueryClient()

  const turnNumber = toCount(interview.answeredCount) + 1

  const form = useForm({
    initialValues: { answer: '' },
    validate: {
      answer: (value) => (value.trim() ? null : 'Answer is required.'),
    },
  })

  const submitMutation = useMutation({
    mutationFn: (values: { answer: string }) =>
      submitAnswer(interviewId, { answer: values.answer, turnNumber }),
    onSuccess: (data) => {
      form.reset()
      queryClient.setQueryData<GetInterviewResponse>(
        ['interview', interviewId],
        data
      )
    },
  })

  const [stopModalOpened, { open: openStopModal, close: closeStopModal }] = useDisclosure(false)

  const stopMutation = useMutation({
    mutationFn: () => completeInterview(interviewId),
    onSuccess: () => {
      closeStopModal()
      void queryClient.invalidateQueries({ queryKey: ['interview', interviewId] })
    },
  })

  const apiErrorMessage =
    submitMutation.error instanceof ApiError
      ? submitMutation.error.message
      : submitMutation.isError
        ? 'Could not submit answer.'
        : null

  const stopErrorMessage =
    stopMutation.error instanceof ApiError
      ? stopMutation.error.message
      : stopMutation.isError
        ? 'Could not stop interview.'
        : null

  return (
    <Stack gap="md">
      <InterviewMeta interview={interview} />
      {interview.currentQuestion && (
        <Card withBorder radius="md">
          <Stack gap="xs">
            <Text size="sm" c="dimmed" fw={500}>
              Question {turnNumber} of {toCount(interview.questionCount)} ·{' '}
              {interview.currentQuestion.topic}
            </Text>
            <Text fw={500}>{interview.currentQuestion.text}</Text>
          </Stack>
        </Card>
      )}
      <form onSubmit={form.onSubmit((values) => submitMutation.mutate(values))}>
        <Stack gap="md">
          {apiErrorMessage && (
            <Alert color="red" title="Could not submit answer">
              {apiErrorMessage}
            </Alert>
          )}
          {stopErrorMessage && (
            <Alert color="red" title="Could not stop interview">
              {stopErrorMessage}
            </Alert>
          )}
          <Textarea
            label="Your answer"
            placeholder="Type your answer here..."
            minRows={5}
            autosize
            disabled={submitMutation.isPending}
            {...form.getInputProps('answer')}
          />
          <Group justify="space-between">
            <Button type="submit" loading={submitMutation.isPending}>
              Submit answer
            </Button>
            <Button
              variant="subtle"
              color="red"
              disabled={submitMutation.isPending}
              onClick={openStopModal}
            >
              Stop interview
            </Button>
          </Group>
        </Stack>
      </form>

      <Modal
        opened={stopModalOpened}
        onClose={closeStopModal}
        title="Stop interview?"
        centered
      >
        <Stack gap="md">
          <Text>Your progress so far will be saved. You won't be able to continue answering.</Text>
          <Group justify="flex-end">
            <Button variant="default" onClick={closeStopModal} disabled={stopMutation.isPending}>
              Cancel
            </Button>
            <Button
              color="red"
              loading={stopMutation.isPending}
              onClick={() => stopMutation.mutate()}
            >
              Stop interview
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  )
}

function CompletedInterview({ interview }: { interview: GetInterviewResponse }) {
  return (
    <Stack gap="md">
      <InterviewMeta interview={interview} />
      <Card withBorder radius="md">
        <Stack gap="xs">
          <Text fw={600}>Interview completed</Text>
          <Text c="dimmed">
            You answered {toCount(interview.answeredCount)} of {toCount(interview.questionCount)}{' '}
            questions.
          </Text>
          <Text size="sm" c="dimmed">
            Feedback will be available here once it has been generated.
          </Text>
        </Stack>
      </Card>
    </Stack>
  )
}
