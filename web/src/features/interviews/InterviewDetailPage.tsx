import { Alert, Badge, Button, Card, Group, Loader, Stack, Text, Textarea, Title } from '@mantine/core'
import { useForm } from '@mantine/form'
import { useState } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError } from '../../api/apiError'
import {
  getInterview,
  startInterview,
  submitAnswer,
  type GetInterviewResponse,
} from '../../api/interviewApi'
import { toCount } from './interviewListHelpers'

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
  const [isStarting, setIsStarting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const handleStart = async () => {
    setErrorMessage(null)
    setIsStarting(true)
    try {
      await startInterview(interviewId)
      await queryClient.invalidateQueries({ queryKey: ['interview', interviewId] })
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'Could not start interview.')
    } finally {
      setIsStarting(false)
    }
  }

  return (
    <Stack gap="md">
      <InterviewMeta interview={interview} />
      {errorMessage && (
        <Alert color="red" title="Could not start interview">
          {errorMessage}
        </Alert>
      )}
      <Group>
        <Button loading={isStarting} onClick={() => void handleStart()}>
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
  const [apiErrorMessage, setApiErrorMessage] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const turnNumber = toCount(interview.answeredCount) + 1

  const form = useForm({
    initialValues: { answer: '' },
    validate: {
      answer: (value) => (value.trim() ? null : 'Answer is required.'),
    },
  })

  const handleSubmit = async (values: { answer: string }) => {
    setApiErrorMessage(null)
    setIsSubmitting(true)
    try {
      await submitAnswer(interviewId, { answer: values.answer, turnNumber })
      form.reset()
      await queryClient.invalidateQueries({ queryKey: ['interview', interviewId] })
    } catch (error) {
      setApiErrorMessage(error instanceof ApiError ? error.message : 'Could not submit answer.')
    } finally {
      setIsSubmitting(false)
    }
  }

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
      <form onSubmit={form.onSubmit((values) => void handleSubmit(values))}>
        <Stack gap="md">
          {apiErrorMessage && (
            <Alert color="red" title="Could not submit answer">
              {apiErrorMessage}
            </Alert>
          )}
          <Textarea
            label="Your answer"
            placeholder="Type your answer here..."
            minRows={5}
            autosize
            disabled={isSubmitting}
            {...form.getInputProps('answer')}
          />
          <Group>
            <Button type="submit" loading={isSubmitting}>
              Submit answer
            </Button>
          </Group>
        </Stack>
      </form>
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
