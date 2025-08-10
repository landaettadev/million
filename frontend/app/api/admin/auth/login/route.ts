import { NextRequest, NextResponse } from 'next/server';

// Mock admin credentials (in production, this should come from your backend)
const MOCK_ADMIN = {
  email: 'admin@millionluxury.com',
  password: 'admin123',
  user: {
    id: '1',
    email: 'admin@millionluxury.com',
    name: 'Admin User',
    role: 'admin',
  }
};

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { email, password } = body;

    // Validate input
    if (!email || !password) {
      return NextResponse.json(
        { error: 'Email and password are required' },
        { status: 400 }
      );
    }

    // Check credentials (mock validation)
    if (email === MOCK_ADMIN.email && password === MOCK_ADMIN.password) {
      // In production, you would validate against your backend here
      // For now, we'll return a mock JWT token
      const mockToken = 'mock-jwt-token-' + Date.now();
      
      return NextResponse.json({
        success: true,
        token: mockToken,
        user: MOCK_ADMIN.user
      });
    }

    // Invalid credentials
    return NextResponse.json(
      { error: 'Invalid email or password' },
      { status: 401 }
    );
  } catch (error) {
    console.error('Login error:', error);
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    );
  }
}
