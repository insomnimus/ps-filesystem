public abstract class Cmd: PSCmdlet {
	internal void CheckCancel() {
		if (this.Stopping) {
			throw new PipelineStoppedException();
		}
	}

	internal string PWD => this.SessionState.Path.CurrentLocation.Path;
}
