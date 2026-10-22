import { describeError } from "../api/errors";

interface ErrorViewProps {
    status?: number;
    correlationId?: string | null;
    retryAfter?: string | null;
    onRetry?: () => void;
}

export function ErrorView({
    status = 500,
    correlationId,
    retryAfter,
    onRetry,
}: ErrorViewProps) {
    return (
        <div role="alert">
            <p>{describeError(status, correlationId, retryAfter)}</p>

            {onRetry && (
                <button type="button" onClick={onRetry}>
                    Retry
                </button>
            )}
        </div>
    );
}