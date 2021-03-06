#pragma once
#include "../System/timer.h"
#include "../OpenGLRenderer/input-handler.h"
#include <string>
#include <vector>
#include <unordered_map>
using namespace std;

class Renderer;
class ExperimentLog;
class Text2D;
class Episode;
class Step;
class Meter2D;
class ViewPort;
class Function;
class FunctionViewer;

enum PlaybackMode {Quarter,Half,Normal,x2,x4};

class SimionLogViewer: public IInputHandler
{
	double m_episodeLength;
	int m_episodeIndex;
	int m_numEpisodes;
	double m_episodeSimTime = 0.0;

	PlaybackMode m_playbackMode= PlaybackMode::Normal;

	Step* 	m_pInterpolatedStep;
	Episode* m_pCurrentEpisode;

	ExperimentLog* m_pExperimentLog= nullptr;
	Renderer* m_pRenderer= nullptr;

	Text2D * m_pTimeText;
	Text2D * m_pEpisodeText;
	Text2D * m_pPlaybackRateText;
	vector <Meter2D*> m_variableMeters;
	ViewPort * m_pMetersViewport;

	unordered_map<FunctionViewer*, Function*> m_functionViews;

	bool m_bExitRequested = false;

	Timer m_timer;

	static void _onKeyPressed(unsigned char key, int x, int y);
	static void _onKeyReleased(unsigned char key, int x, int y);
	static void _onSpecialKeyPressed(int key, int x, int y);
	static void _onSpecialKeyReleased(int key, int x, int y);

	void onKeyPressed(unsigned char key, int x, int y);
	void onKeyReleased(unsigned char key, int x, int y);
	void onSpecialKeyPressed(int key, int x, int y);
	void onSpecialKeyReleased(int key, int x, int y);

	void nextEpisode();
	void previousEpisode();
	void playEpisode(int i);

	void faster();
	void slower();
	double getPlaybackRate();

	void interpolateStepData(double t, Episode* pInEpisode, Step* pOutInterpolatedData) const;
public:
	SimionLogViewer();
	virtual ~SimionLogViewer();

	void init(int argc, char** argv);
	bool loadLogFile(string filename);

	bool bExitRequested() const { return m_bExitRequested; }

	//drawing
	void draw();

	//input handling
	void handleInput();
};

