import type { InterviewSummary, InterviewStatusContract } from '../../api/interviewApi'

export function formatProgress(interview: InterviewSummary): string {
  const { answeredCount, questionCount } = interview

  if (questionCount <= 0) {
    return '0/0'
  }

  return `${answeredCount}/${questionCount}`
}

export function statusColor(status: InterviewStatusContract): string {
  switch (status) {
    case 'Active':
      return 'blue'
    case 'Completed':
      return 'green'
    default:
      return 'gray'
  }
}

export function statusAction(status: InterviewStatusContract): string {
  switch (status) {
    case 'Active':
      return 'Continue'
    case 'Completed':
      return 'View'
    default:
      return 'Open'
  }
}
