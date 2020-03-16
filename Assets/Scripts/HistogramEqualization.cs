using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class HistogramEqualization : MonoBehaviour
{
    private const int HISTOGRAM_BINS = 256;

    public ComputeShader GrayScaleComputeShader;
    public ComputeShader HistogramShader;
    public ComputeShader HistogramCDFShader;
    public ComputeShader HistogramTransfromShader;

    private RenderTexture tmp1, tmp2,tmp3,tmp4;

    private CommandBuffer commandBuffer = null;
    private ComputeBuffer histogramBuffer = null;
    private ComputeBuffer funcBuffer = null;

    private int grayScaleKernelID;
    private int histogramKernelID;
    private int initHistogramKernelID;

    private int histogramCDFID;
    private int histogramTransfromID;


    // Start is called before the first frame update
    void Start()
    {
        grayScaleKernelID = GrayScaleComputeShader.FindKernel("ToGrayScale");
        histogramKernelID = HistogramShader.FindKernel("ComputeHistogram");
        initHistogramKernelID = HistogramShader.FindKernel("InitHistogram");
        histogramCDFID = HistogramCDFShader.FindKernel("HistogramCDF");
        histogramTransfromID = HistogramTransfromShader.FindKernel("HistogramTransfromCDF");
        histogramBuffer = new ComputeBuffer(HISTOGRAM_BINS, sizeof(uint));
        funcBuffer = new ComputeBuffer(HISTOGRAM_BINS, sizeof(uint));
    }

    private void Update()
    {
    
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        InitRenderTexture(source);

        if (commandBuffer == null)
        {
            commandBuffer = new CommandBuffer();
        }
        else
        {
            commandBuffer.Clear();
        }
        commandBuffer.SetComputeTextureParam(GrayScaleComputeShader, grayScaleKernelID, "Source", source);
        commandBuffer.SetComputeTextureParam(GrayScaleComputeShader, grayScaleKernelID, "Result", tmp1);
        commandBuffer.DispatchCompute(GrayScaleComputeShader, grayScaleKernelID, source.width, source.height, 1);


        commandBuffer.SetComputeBufferParam(HistogramShader, initHistogramKernelID, "Histogram", histogramBuffer);
        commandBuffer.DispatchCompute(HistogramShader, initHistogramKernelID, HISTOGRAM_BINS, 1, 1);

        commandBuffer.SetComputeTextureParam(HistogramShader, histogramKernelID, "Source", tmp1);
        commandBuffer.SetComputeBufferParam(HistogramShader, histogramKernelID, "Histogram", histogramBuffer);
        commandBuffer.DispatchCompute(HistogramShader, histogramKernelID, Mathf.CeilToInt(source.width/16f), Mathf.CeilToInt(source.height / 16f), 1);

        
        commandBuffer.SetComputeBufferParam(HistogramCDFShader, histogramCDFID, "SourceHistogram", histogramBuffer);
        commandBuffer.SetComputeBufferParam(HistogramCDFShader, histogramCDFID, "ResultFunc", funcBuffer);
        commandBuffer.DispatchCompute(HistogramCDFShader, histogramCDFID, 1, 1, 1);

        
        commandBuffer.SetComputeTextureParam(HistogramTransfromShader, histogramTransfromID, "Source", tmp1);
        commandBuffer.SetComputeTextureParam(HistogramTransfromShader, histogramTransfromID, "Result", tmp2);
        commandBuffer.SetComputeBufferParam(HistogramTransfromShader, histogramTransfromID, "TransfromFunc", funcBuffer);
        commandBuffer.SetComputeFloatParam(HistogramTransfromShader, "SourceSize", source.width * source.height);

        commandBuffer.DispatchCompute(HistogramTransfromShader, histogramTransfromID, Mathf.CeilToInt(source.width / 8f), Mathf.CeilToInt(source.height / 8f), 1);

        commandBuffer.Blit(tmp2, destination);

        Graphics.ExecuteCommandBuffer(commandBuffer);
    }

    private void OnDestroy()
    {
        if (tmp1 != null)
            tmp1.Release();
        if (tmp2 != null)
            tmp2.Release();
        if (tmp3 != null)
            tmp3.Release();
        if (tmp4 != null)
            tmp4.Release();
        if (histogramBuffer != null)
        {
            histogramBuffer.Release();
        }
    }

    private void InitRenderTexture(RenderTexture source)
    {
        if (tmp1 == null || tmp1.width != source.width || tmp1.height != source.height)
        {
            // Release render texture if we already have one
            if (tmp1 != null)
                tmp1.Release();
            if (tmp2 != null)
                tmp2.Release();
            if (tmp3 != null)
                tmp3.Release();
            if (tmp4 != null)
                tmp4.Release();
            // Get a render target for Ray Tracing
            tmp1 = new RenderTexture(source.width, source.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            tmp1.enableRandomWrite = true;
            tmp1.Create();

            tmp2 = new RenderTexture(source.width, source.height, 0,
               RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            tmp2.enableRandomWrite = true;
            tmp2.Create();


            tmp3 = new RenderTexture(source.width, source.height, 0,
               RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            tmp3.enableRandomWrite = true;
            tmp3.Create();

            tmp4 = new RenderTexture(source.width, source.height, 0,
             RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            tmp4.enableRandomWrite = true;
            tmp4.Create();
        }
    }
}
