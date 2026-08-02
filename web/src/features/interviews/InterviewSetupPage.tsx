import {
  Alert,
  Button,
  Card,
  Group,
  NumberInput,
  Select,
  Stack,
  Text,
  TextInput,
  Title,
} from '@mantine/core'
import { useForm } from '@mantine/form'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '../../api/apiError'
import { extractFieldErrors } from '../../api/serverValidation'
import { createInterview, type CreateInterviewRequest } from '../../api/interviewApi'
import {
  defaultQuestionCount,
  focusAreaOptions,
  interviewTypeOptions,
  questionCountMax,
  questionCountMin,
  questionCountOptions,
  roleOptions,
  seniorityOptions,
} from './interviewOptions'

export type SetupFormValues = {
  targetRole: string
  focusArea: string
  interviewType: string
  seniorityLevel: string
  questionCount: number
}

const initialValues: SetupFormValues = {
  targetRole: '',
  focusArea: '',
  interviewType: '',
  seniorityLevel: '',
  questionCount: defaultQuestionCount,
}

export function InterviewSetupPage() {
  const navigate = useNavigate()
  const [apiErrorMessage, setApiErrorMessage] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const form = useForm<SetupFormValues>({
    initialValues,
    validateInputOnBlur: true,
    validate: {
      targetRole: (value) => (value.trim() ? null : 'Role is required.'),
      focusArea: (value) => (value.trim() ? null : 'Topic is required.'),
      interviewType: (value) => (value.trim() ? null : 'Interview type is required.'),
      seniorityLevel: (value) => (value.trim() ? null : 'Seniority is required.'),
      questionCount: (value) =>
        Number.isFinite(value) && value >= questionCountMin && value <= questionCountMax
          ? null
          : `Question count must be between ${questionCountMin} and ${questionCountMax}.`,
    },
  })

  const handleSubmit = async (values: SetupFormValues) => {
    setApiErrorMessage(null)
    setIsSubmitting(true)

    try {
      const request: CreateInterviewRequest = {
        targetRole: values.targetRole,
        focusArea: values.focusArea,
        interviewType: values.interviewType,
        seniorityLevel: values.seniorityLevel,
        questionCount: values.questionCount,
      }

      const created = await createInterview(request)
      await navigate(`/interviews/${created.id}`)
    } catch (error) {
      // Prefer mapping server-side validation errors back onto their fields.
      const fieldErrors = extractFieldErrors(error)

      if (Object.keys(fieldErrors).length > 0) {
        form.setErrors(fieldErrors)
      } else {
        const message = error instanceof ApiError ? error.message : 'Failed to create interview.'
        setApiErrorMessage(message)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Stack gap="md">
      <Title>New Interview</Title>
      <Text c="dimmed">Configure your interview settings before you start.</Text>

      <Card withBorder radius="md">
        <form onSubmit={form.onSubmit((values) => void handleSubmit(values))}>
          <Stack gap="md">
            {apiErrorMessage && (
              <Alert color="red" title="Could not create interview">
                {apiErrorMessage}
              </Alert>
            )}

            <TextInput
              label="Role"
              placeholder="Enter target role"
              data-list="interview-role-options"
              disabled={isSubmitting}
              required
              {...form.getInputProps('targetRole')}
            />
            <datalist id="interview-role-options">
              {roleOptions.map((role) => (
                <option key={role} value={role} />
              ))}
            </datalist>

            <Select
              label="Topic"
              placeholder="Select focus area"
              data={[...focusAreaOptions]}
              disabled={isSubmitting}
              required
              {...form.getInputProps('focusArea')}
            />

            <Select
              label="Interview type"
              placeholder="Select interview type"
              data={[...interviewTypeOptions]}
              disabled={isSubmitting}
              required
              {...form.getInputProps('interviewType')}
            />

            <Select
              label="Seniority"
              placeholder="Select seniority"
              data={[...seniorityOptions]}
              disabled={isSubmitting}
              required
              {...form.getInputProps('seniorityLevel')}
            />

            <NumberInput
              label="Question count"
              placeholder="Select question count"
              min={questionCountMin}
              max={questionCountMax}
              disabled={isSubmitting}
              required
              {...form.getInputProps('questionCount')}
            />

            <Group justify="space-between">
              <Button
                variant="subtle"
                onClick={() => void navigate('/interviews')}
                disabled={isSubmitting}
              >
                Cancel
              </Button>

              <Button type="submit" loading={isSubmitting}>
                Start interview
              </Button>
            </Group>
          </Stack>
        </form>
      </Card>

      <Text size="sm" c="dimmed">
        Allowed range is {questionCountMin} to {questionCountMax}. Suggested question counts:{' '}
        {questionCountOptions.join(', ')}.
      </Text>
    </Stack>
  )
}
