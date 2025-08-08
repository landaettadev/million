import { AlertTriangle, RefreshCw, Wifi, WifiOff } from 'lucide-react';
import { Button } from './Button';
import { getErrorMessage } from '../../lib/errors';

interface ErrorMessageProps {
  error: unknown;
  onRetry?: () => void;
  className?: string;
}

export function ErrorMessage({ error, onRetry, className = '' }: ErrorMessageProps) {
  const message = getErrorMessage(error);
  
  // Determine icon and styling based on error type
  const getErrorConfig = () => {
    const errorStr = String(error);
    
    if (errorStr.includes('network') || errorStr.includes('fetch')) {
      return {
        icon: WifiOff,
        title: 'Connection Error',
        color: 'text-red-500',
        bgColor: 'bg-red-50',
        borderColor: 'border-red-200'
      };
    }
    
    if (errorStr.includes('timeout')) {
      return {
        icon: RefreshCw,
        title: 'Request Timeout',
        color: 'text-orange-500',
        bgColor: 'bg-orange-50',
        borderColor: 'border-orange-200'
      };
    }
    
    return {
      icon: AlertTriangle,
      title: 'Error',
      color: 'text-red-500',
      bgColor: 'bg-red-50',
      borderColor: 'border-red-200'
    };
  };

  const config = getErrorConfig();
  const Icon = config.icon;

  return (
    <div className={`rounded-2xl border ${config.borderColor} ${config.bgColor} p-6 ${className}`}>
      <div className="flex items-start space-x-4">
        <div className={`flex-shrink-0 ${config.color}`}>
          <Icon className="h-6 w-6" />
        </div>
        <div className="flex-1 min-w-0">
          <h3 className="text-lg font-medium text-gray-900 mb-1">
            {config.title}
          </h3>
          <p className="text-gray-600 text-sm leading-relaxed">
            {message}
          </p>
          {onRetry && (
            <div className="mt-4">
              <Button 
                onClick={onRetry}
                variant="primary"
                size="sm"
              >
                Try Again
              </Button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

// Compact version for inline errors
export function ErrorMessageInline({ error, onRetry, className = '' }: ErrorMessageProps) {
  const message = getErrorMessage(error);
  
  return (
    <div className={`flex items-center space-x-2 text-red-600 text-sm ${className}`}>
      <AlertTriangle className="h-4 w-4 flex-shrink-0" />
      <span className="flex-1">{message}</span>
      {onRetry && (
        <Button 
          onClick={onRetry}
          variant="ghost"
          size="sm"
          className="text-red-600 hover:text-red-700"
        >
          Retry
        </Button>
      )}
    </div>
  );
}
