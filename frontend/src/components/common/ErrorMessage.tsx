interface ErrorMessageProps {
  message: string;
  onRetry?: () => void;
}

export const ErrorMessage = ({ message, onRetry }: ErrorMessageProps) => {
  return (
    <div className="error-message">
      <p>{message}</p>
      {onRetry && (
        <button onClick={onRetry} className="btn btn-primary">
          Retry
        </button>
      )}
    </div>
  );
};
