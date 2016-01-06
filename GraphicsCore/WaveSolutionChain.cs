using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using D3D11 = SharpDX.Direct3D11;

namespace GraphicsCore
{
    /**
     * Mechanism to neatly swap two buffers sent to Compute Shader. This class handles the task of swapping two arrays CurrentSolution and PastSolution in Original Wave Code in http://richardssoftware.net/ 
     */

    class WaveSolutionSwapChain
    {

        private D3D11.UnorderedAccessView currentSolutionUAV;
        private D3D11.UnorderedAccessView pastSolutionUAV;
        private D3D11.ShaderResourceView currentSolutionSRV;
        private D3D11.ShaderResourceView pastSolutionSRV;
        private D3D11.Buffer currentBuffer;
        private D3D11.Buffer pastBuffer;

        public WaveSolutionSwapChain(D3D11.ShaderResourceView currentSolutionSRV, D3D11.UnorderedAccessView currentSolutionUAV, D3D11.Buffer currentBuffer, D3D11.ShaderResourceView pastSolutionSRV, D3D11.UnorderedAccessView pastSolutionUAV, D3D11.Buffer pastBuffer)
        {
            this.currentSolutionSRV = currentSolutionSRV;
            this.pastSolutionSRV = pastSolutionSRV;
            this.currentSolutionUAV = currentSolutionUAV;
            this.pastSolutionUAV = pastSolutionUAV;
            this.currentBuffer = currentBuffer;
            this.pastBuffer = pastBuffer;
        }

        public D3D11.UnorderedAccessView CurrentSolution { get { return currentSolutionUAV; } } //used to pass to Compute Shader
        public D3D11.ShaderResourceView PastSolution { get { return pastSolutionSRV; } } //used to pass to Compute Shader
        public D3D11.ShaderResourceView CurrentSRV { get { return currentSolutionSRV; } } //used to pass to Vertex Shader when rendering water
        public D3D11.Buffer CurrentBuffer { get { return currentBuffer; } } //used to manipulate and introduce disturbances to water by code
        public void Swap()
        {
            var tempSRV = currentSolutionSRV;
            currentSolutionSRV = pastSolutionSRV;
            pastSolutionSRV = tempSRV;

            var tempUAV = currentSolutionUAV;
            currentSolutionUAV = pastSolutionUAV;
            pastSolutionUAV = tempUAV;

            var tempBuffer = currentBuffer;
            currentBuffer = pastBuffer;
            pastBuffer = tempBuffer;
        }
    }
        
}
