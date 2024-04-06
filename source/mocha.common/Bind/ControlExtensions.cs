using System.Linq.Expressions;
using System.Reflection;

public static class ObjectExtensions
{
	private static readonly Dictionary<object, MirrorState> MirrorStates = new Dictionary<object, MirrorState>();

	public static void Mirror<T1, T2>( this T1 source, Expression<Func<T1, object>> sourcePropertyExpression, T2 target, Expression<Func<T2, object>> targetPropertyExpression )
	{
		var sourceProperty = ExtractPropertyInfo( sourcePropertyExpression );
		var targetProperty = ExtractPropertyInfo( targetPropertyExpression );

		if ( sourceProperty == null )
		{
			throw new ArgumentException( "Invalid source property.", nameof( sourcePropertyExpression ) );
		}

		if ( targetProperty == null )
		{
			throw new ArgumentException( "Invalid target property.", nameof( targetPropertyExpression ) );
		}

		object currentValue = sourceProperty.GetValue( source );

		// Replace the existing mirror state if there is one
		if ( MirrorStates.ContainsKey( source ) )
		{
			MirrorStates[source].Stop();
		}

		var mirrorState = new MirrorState( source, target, sourceProperty, targetProperty, currentValue );
		MirrorStates[source] = mirrorState;
	}

	public static void StopMirror<T>( this T source )
	{
		if ( MirrorStates.TryGetValue( source, out var mirrorState ) )
		{
			mirrorState.Stop();
			MirrorStates.Remove( source );
		}
	}

	public static void Update()
	{
		foreach ( var mirrorState in MirrorStates.Values )
		{
			mirrorState.Update();
		}
	}

	private static PropertyInfo ExtractPropertyInfo<T>( Expression<Func<T, object>> expression )
	{
		if ( expression.Body is MemberExpression member )
		{
			return (PropertyInfo)member.Member;
		}

		if ( expression.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember )
		{
			return (PropertyInfo)unaryMember.Member;
		}

		return null;
	}

	private class MirrorState
	{
		private readonly object _source;
		private readonly object _target;
		private readonly PropertyInfo _sourceProperty;
		private readonly PropertyInfo _targetProperty;
		private object _currentValue;

		public MirrorState( object source, object target, PropertyInfo sourceProperty, PropertyInfo targetProperty, object initialValue )
		{
			this._source = source;
			this._target = target;
			this._sourceProperty = sourceProperty;
			this._targetProperty = targetProperty;
			this._currentValue = initialValue;
		}

		public void Update()
		{
			var newValue = _sourceProperty.GetValue( _source );
			if ( !Equals( _currentValue, newValue ) )
			{
				_targetProperty.SetValue( _target, newValue );
				_currentValue = newValue;
			}
		}

		public void Stop() {}
	}
}
