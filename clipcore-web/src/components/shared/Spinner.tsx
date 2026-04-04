import React from 'react';

interface Props {
  size?: 'sm' | 'md' | 'lg';
  center?: boolean;
}

export default function Spinner({ size = 'md', center = false }: Props) {
  const cls = `spinner${size === 'lg' ? ' spinner-lg' : ''}`;
  if (center) {
    return (
      <div className="spinner-center">
        <div className={cls} />
      </div>
    );
  }
  return <div className={cls} />;
}
