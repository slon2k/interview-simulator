import {
  Alert,
  Accordion,
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
  getInterviewDetails,
  type InterviewResponse,
  type GetInterviewDetailsResponse,
} from '../../api/interviewApi'

type InterviewQuery = ReturnType<typeof useQuery<InterviewResponse>>

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

  if (interview.status === 'Created') {
    return <CreatedInterview interviewId={interviewId} interview={interview} />
  }

  if (interview.status === 'Active') {
    return <ActiveInterview interviewId={interviewId} interview={interview} />
  }

  if (interview.status === 'Completed') {
    return <CompletedInterview interview={interview} />
  }

  return <Text c="dimmed">Unknown interview status: {interview.status}</Text>
}

function InterviewMeta({ interview }: { interview: InterviewResponse }) {
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
            {interview.answeredCount} / {interview.questionCount} questions
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
  interview: InterviewResponse
}) {
  const queryClient = useQueryClient()

  const startMutation = useMutation({
    mutationFn: () => startInterview(interviewId),
    onSuccess: (data) =>
      queryClient.setQueryData<InterviewResponse>(['interview', interviewId], {
        ...data,
        totalScore: null,
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
  interview: InterviewResponse
}) {
  const queryClient = useQueryClient()

  const form = useForm({
    initialValues: { answer: '' },
    validate: {
      answer: (value) => (value.trim() ? null : 'Answer is required.'),
    },
  })

  const submitMutation = useMutation({
    mutationFn: (values: { answer: string }) =>
      submitAnswer(interviewId, {
        answer: values.answer,
        turnNumber: interview.currentQuestion?.turnNumber ?? interview.answeredCount + 1,
      }),
    onSuccess: (data) => {
      form.reset()
      queryClient.setQueryData<InterviewResponse>(['interview', interviewId], data)
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
              Question {interview.currentQuestion.turnNumber} of {interview.questionCount} ·{' '}
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

      <Modal opened={stopModalOpened} onClose={closeStopModal} title="Stop interview?" centered>
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

function CompletedInterview({ interview }: { interview: InterviewResponse }) {
  const detailsQuery = useQuery({
    queryKey: ['interview-details', interview.id],
    queryFn: ({ signal }: { signal: AbortSignal }) => getInterviewDetails(interview.id, signal),
    enabled: interview.status === 'Completed',
  })

  if (detailsQuery.isLoading) {
    return (
      <Stack gap="md">
        <InterviewMeta interview={interview} />
        <Group>
          <Loader size="sm" />
          <Text c="dimmed">Loading interview feedback...</Text>
        </Group>
      </Stack>
    )
  }

  if (detailsQuery.isError) {
    const message =
      detailsQuery.error instanceof ApiError
        ? detailsQuery.error.message
        : 'Unable to load interview feedback.'

    return (
      <Stack gap="md">
        <InterviewMeta interview={interview} />
        <Alert color="red" title="Could not load interview feedback">
          {message}
        </Alert>
        <Group>
          <Button variant="light" onClick={() => void detailsQuery.refetch()}>
            Retry
          </Button>
        </Group>
      </Stack>
    )
  }

  const details = detailsQuery.data

  return (
    <Stack gap="md">
      <InterviewMeta interview={interview} />
      <Card withBorder radius="md">
        <Stack gap="xs">
          <Group justify="space-between" align="center" wrap="wrap" gap="xs">
            <Text fw={600}>Interview completed</Text>
            <Badge color={scoreColor(details?.totalScore)} variant="light" size="lg">
              {details?.totalScore !== null && details?.totalScore !== undefined
                ? `Total score: ${details.totalScore}/100`
                : 'Total score: Pending'}
            </Badge>
          </Group>
          <Text c="dimmed">
            You answered {interview.answeredCount} of {interview.questionCount} questions.
          </Text>
          <Text size="sm">{details?.summary?.text ?? 'Summary pending...'}</Text>
        </Stack>
      </Card>
      {details && <CompletedTurnList details={details} />}
    </Stack>
  )
}

function scoreColor(score: number | null | undefined): string {
  if (score === null || score === undefined) {
    return 'gray'
  }

  if (score < 50) {
    return 'red'
  }

  if (score < 75) {
    return 'yellow'
  }

  return 'green'
}

function CompletedTurnList({ details }: { details: GetInterviewDetailsResponse }) {
  return (
    <Stack gap="sm">
      <Title order={3}>Turn-by-turn feedback</Title>
      <Accordion variant="separated">
        {details.turns.map((turn) => (
          <Accordion.Item key={turn.turnNumber} value={String(turn.turnNumber)}>
            <Accordion.Control>
              <Group justify="space-between" wrap="nowrap" pr="sm">
                <Text fw={500} style={{ minWidth: 0, flex: 1 }}>
                  Question {turn.turnNumber}: {turn.question.text}
                </Text>
                <Badge color="blue" variant="light" style={{ flexShrink: 0, minWidth: 76 }}>
                  {turn.evaluation ? `${turn.evaluation.overallScore}/100` : 'Not scored'}
                </Badge>
              </Group>
            </Accordion.Control>
            <Accordion.Panel>
              <Stack gap="sm">
                <div>
                  <Text size="sm" fw={600}>
                    Answer
                  </Text>
                  <Text>{turn.answer?.text ?? 'No answer provided.'}</Text>
                </div>
                {turn.evaluation && (
                  <Stack gap="xs">
                    <Text size="sm" fw={600}>
                      Evaluation
                    </Text>
                    <Text size="sm">{turn.evaluation.overallFeedback}</Text>
                    {turn.evaluation.dimensions.map((dimension) => (
                      <Card key={dimension.key} withBorder radius="sm" p="sm">
                        <Group justify="space-between">
                          <Text fw={500}>{dimension.label}</Text>
                          <Badge variant="light" style={{ flexShrink: 0, minWidth: 58 }}>
                            {dimension.score}/100
                          </Badge>
                        </Group>
                        <Text size="sm" c="dimmed">
                          {dimension.feedback}
                        </Text>
                      </Card>
                    ))}
                  </Stack>
                )}
              </Stack>
            </Accordion.Panel>
          </Accordion.Item>
        ))}
      </Accordion>
    </Stack>
  )
}
