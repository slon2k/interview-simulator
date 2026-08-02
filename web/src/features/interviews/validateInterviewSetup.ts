export type SetupFormValues = {
  targetRole: string
  focusArea: string
  interviewType: string
  seniorityLevel: string
  questionCount: number
}

export type SetupFormErrors = Partial<Record<keyof SetupFormValues, string>>

export function validate(values: SetupFormValues): SetupFormErrors {
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
