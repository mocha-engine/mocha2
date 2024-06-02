
namespace Mocha.Rendering.Vulkan;

class VulkanDeletionQueue
{
	public Queue<Action> Queue;

	public VulkanDeletionQueue()
	{
		Queue = new();
	}

	public void Enqueue( Action function )
	{
		Queue.Enqueue( function );
	}

	public void Flush()
	{
		foreach ( var item in Queue )
		{
			item.Invoke();
		}

		Queue.Clear();
	}
}
