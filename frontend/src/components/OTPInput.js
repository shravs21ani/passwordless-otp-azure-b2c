import React, { useRef, useEffect } from 'react';

const OTPInput = ({ value, onChange, length = 6, disabled = false }) => {
  const inputRefs = useRef([]);

  useEffect(() => {
    // Auto-focus first empty input
    const firstEmptyIndex = value.length;
    if (firstEmptyIndex < length && inputRefs.current[firstEmptyIndex]) {
      inputRefs.current[firstEmptyIndex].focus();
    }
  }, [value, length]);

  const handleChange = (index, digit) => {
    if (disabled) return;

    // Only allow numbers
    if (!/^\d$/.test(digit)) return;

    const newValue = value.split('');
    newValue[index] = digit;
    onChange(newValue.join(''));

    // Move to next input if available
    if (index < length - 1 && digit) {
      inputRefs.current[index + 1]?.focus();
    }
  };

  const handleKeyDown = (index, e) => {
    if (disabled) return;

    if (e.key === 'Backspace') {
      if (value[index]) {
        // Clear current input
        const newValue = value.split('');
        newValue[index] = '';
        onChange(newValue.join(''));
      } else if (index > 0) {
        // Move to previous input and clear it
        const newValue = value.split('');
        newValue[index - 1] = '';
        onChange(newValue.join(''));
        inputRefs.current[index - 1]?.focus();
      }
    } else if (e.key === 'ArrowLeft' && index > 0) {
      inputRefs.current[index - 1]?.focus();
    } else if (e.key === 'ArrowRight' && index < length - 1) {
      inputRefs.current[index + 1]?.focus();
    }
  };

  const handlePaste = (e) => {
    if (disabled) return;

    e.preventDefault();
    const pastedData = e.clipboardData.getData('text/plain');
    const numbers = pastedData.replace(/\D/g, '').slice(0, length);
    
    if (numbers.length > 0) {
      onChange(numbers.padEnd(length, ''));
      // Focus last filled input or first empty input
      const focusIndex = Math.min(numbers.length, length - 1);
      inputRefs.current[focusIndex]?.focus();
    }
  };

  return (
    <div className="flex justify-center space-x-2">
      {Array.from({ length }, (_, index) => (
        <input
          key={index}
          ref={(el) => (inputRefs.current[index] = el)}
          type="text"
          inputMode="numeric"
          pattern="[0-9]*"
          maxLength={1}
          value={value[index] || ''}
          onChange={(e) => handleChange(index, e.target.value)}
          onKeyDown={(e) => handleKeyDown(index, e)}
          onPaste={handlePaste}
          disabled={disabled}
          className="w-12 h-12 text-center text-xl font-semibold border-2 border-gray-300 rounded-lg focus:border-primary-500 focus:ring-2 focus:ring-primary-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          autoComplete="one-time-code"
        />
      ))}
    </div>
  );
};

export default OTPInput;

