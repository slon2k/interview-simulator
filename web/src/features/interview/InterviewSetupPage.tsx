import { useState } from 'react'
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
import { useNavigate } from 'react-router-dom'
import { ApiError } from '../../api/apiError'
import { createInterview, type CreateInterviewRequest } from '../../api/interviewApi'

type SetupFormValues = {
  targetRole: string
  focusArea: string
  interviewType: string
  seniorityLevel: string
  questionCount: number
}

type SetupFormErrors = Partial<Record<keyof SetupFormValues, string>>

const roleOptions = [
  'Backend Engineer',
  'Frontend Engineer',
  'Full Stack Engineer',
  'QA Engineer',
  'DevOps Engineer',
]

const focusAreaOptions = ['dotnet', 'javascript', 'typescript', 'react', 'sql', 'system-design']

const interviewTypeOptions = ['Technical', 'Behavioral', 'SystemDesign']
const seniorityOptions = ['Junior', 'Middle', 'Senior']
const questionCountOptions = [3, 5, 7, 10]

const initialValues: SetupFormValues = {
  targetRole: '',
  focusArea: '',
  interviewType: '',
  seniorityLevel: '',
  questionCount: 0,
}

export function InterviewSetupPage() {
  const navigate = useNavigate()
  const [values, setValues] = useState<SetupFormValues>(initialValues)
  const [errors, setErrors] = useState<SetupFormErrors>({})
  const [apiErrorMessage, setApiErrorMessage] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const clearFieldError = (field: keyof SetupFormValues) => {
    setErrors((current) => {
      if (!current[field]) {
        return current
      }

      const next = { ...current }
      delete next[field]
      return next
    })
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const validationErrors = validate(values)
    setErrors(validationErrors)
    setApiErrorMessage(null)

    if (Object.keys(validationErrors).length > 0) {
      return
    }

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
      navigate(`/interviews/${created.id}`)
    } catch (error) {
      const apiError =
        error instanceof ApiError ? error : new ApiError(0, 'Failed to create interview')
      setApiErrorMessage(apiError.message)
    } finally {
      setIsSubmitting(false)
    }
  }

  const isDisabled = isSubmitting

  return (
    <Stack gap="md">
      <Title>New Interview</Title>
      <Text c="dimmed">Configure your interview settings before you start.</Text>

      <Card withBorder radius="md">
        <form onSubmit={(event) => void handleSubmit(event)}>
          <Stack gap="md">
            {apiErrorMessage && (
              <Alert color="red" title="Could not create interview">
                {apiErrorMessage}
              </Alert>
            )}

            <TextInput
              label="Role"
              placeholder="Enter target role"
              value={values.targetRole}
              data-list="interview-role-options"
              onChange={(event) => {
                const nextRole = event.currentTarget.value
                setValues((current) => ({ ...current, targetRole: nextRole }))
                clearFieldError('targetRole')
              }}
              error={errors.targetRole}
              disabled={isDisabled}
              required
            />
            <datalist id="interview-role-options">
              {roleOptions.map((role) => (
                <option key={role} value={role} />
              ))}
            </datalist>

            <Select
              label="Topic"
              placeholder="Select focus area"
              data={focusAreaOptions}
              value={values.focusArea}
              onChange={(value) => {
                setValues((current) => ({ ...current, focusArea: value ?? '' }))
                clearFieldError('focusArea')
              }}
              error={errors.focusArea}
              disabled={isDisabled}
              required
            />

            <Select
              label="Interview type"
              placeholder="Select interview type"
              data={interviewTypeOptions}
              value={values.interviewType}
              onChange={(value) => {
                setValues((current) => ({ ...current, interviewType: value ?? '' }))
                clearFieldError('interviewType')
              }}
              error={errors.interviewType}
              disabled={isDisabled}
              required
            />

            <Select
              label="Seniority"
              placeholder="Select seniority"
              data={seniorityOptions}
              value={values.seniorityLevel}
              onChange={(value) => {
                setValues((current) => ({ ...current, seniorityLevel: value ?? '' }))
                clearFieldError('seniorityLevel')
              }}
              error={errors.seniorityLevel}
              disabled={isDisabled}
              required
            />

            <NumberInput
              label="Question count"
              placeholder="Select question count"
              value={values.questionCount}
              onChange={(value) => {
                setValues((current) => ({
                  ...current,
                  questionCount: typeof value === 'number' ? value : 0,
                }))
                clearFieldError('questionCount')
              }}
              error={errors.questionCount}
              disabled={isDisabled}
              min={1}
              max={20}
              required
            />

            <Group justify="space-between">
              <Button
                variant="subtle"
                onClick={() => navigate('/interviews')}
                disabled={isDisabled}
              >
                Cancel
              </Button>

              <Button type="submit" loading={isSubmitting} disabled={isDisabled || Object.keys(errors).length > 0}>
                Start interview
              </Button>
            </Group>
          </Stack>
        </form>
      </Card>

      <Text size="sm" c="dimmed">
        Suggested question counts: {questionCountOptions.join(', ')}
      </Text>
    </Stack>
  )
}

function validate(values: SetupFormValues): SetupFormErrors {
  const nextErrors: SetupFormErrors = {}

  if (!values.targetRole.trim()) {
    nextErrors.targetRole = 'Role is required.'
  }

  if (!values.focusArea.trim()) {
    nextErrors.focusArea = 'Topic is required.'
  }

  if (!values.interviewType.trim()) {
    nextErrors.interviewType = 'Interview type is required.'
  }

  if (!values.seniorityLevel.trim()) {
    nextErrors.seniorityLevel = 'Seniority is required.'
  }

  if (!Number.isFinite(values.questionCount) || values.questionCount < 1) {
    nextErrors.questionCount = 'Question count must be greater than zero.'
  }

  return nextErrors
}
