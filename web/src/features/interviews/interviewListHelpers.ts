import type { InterviewSummary } from '../../api/interviewApi'
import { toCount } from './interviewHelpers'

export { toCount } from './interviewHelpers'

export function formatProgress(interview: InterviewSummary): string {
  const answered = toCount(interview.answeredCount)
  const total = toCount(interview.questionCount)

  if (total <= 0) {
    return '0/0'
  }

  return `${answered}/${total}`
}

export function statusColor(status: string): string {
  switch (status) {
    case 'active':
      return 'blue'
    case 'completed':
      return 'green'
    default:
      return 'gray'
  }
}

export function statusAction(status: string): string {
  switch (status) {
    case 'active':
      return 'Continue'
    case 'completed':
      return 'View'
    default:
      return 'Open'
  }
}
